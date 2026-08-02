import inspect
from dataclasses import is_dataclass
from typing import Callable, Any
import sys
import base64
import json
from ._action_dtos import ActionRequest, ActionResponse

ON_STARTUP_SUCCESS = "[AmenoLink.StartupSuccess]"
ON_ACTION_SUCCESS = "[AmenoLink.ActionSuccess]"
ON_ACTION_ERROR = "[AmenoLink.ActionError]"
ON_ACTION_LOGGED = "[AmenoLink.ActionLog]"

type ActionHandler = Callable[..., str]
type ActionRoute = tuple[str, ActionHandler]


class ActionContext:
    def __init__(self, request: ActionRequest):
        self.request = request

    def log(self, message: str):
        send_message(ON_ACTION_LOGGED, message)


class ActionRouter:
    def __init__(self):
        self.routes: list[ActionRoute] = []

    def add(self, route: str, handler: ActionHandler):
        sig = inspect.signature(handler)
        params = list(sig.parameters.values())

        if not params or params[0].annotation == inspect.Parameter.empty:
            raise TypeError(f"A função da rota '{route}' deve declarar o tipo do parâmetro de entrada.")

        if sig.return_annotation == inspect.Signature.empty:
            raise TypeError(f"A função da rota '{route}' deve declarar o tipo de retorno.")

        self.routes.append((route, handler))

    def __execute(self, request: ActionRequest) -> str:
        for route_name, handler in self.routes:
            if route_name == request.route:
                sig = inspect.signature(handler)
                params = list(sig.parameters.values())
                param_type = params[0].annotation

                arg = self._parse_payload(request.payload, param_type)
                raw_result = handler(arg)
                return self._format_result(raw_result)

        raise ValueError(f"Rota '{request.route}' não encontrada")

    def _parse_payload(self, payload: str, param_type: type) -> Any:
        if param_type == str:
            return payload
        if hasattr(param_type, 'from_json'):
            return param_type.from_json(payload)
        if hasattr(param_type, 'from_dict'):
            return param_type.from_dict(json.loads(payload))
        if is_dataclass(param_type):
            return param_type(**json.loads(payload))
        return param_type(payload)

    def _format_result(self, raw_result: Any) -> str:
        if isinstance(raw_result, str):
            return raw_result
        if hasattr(raw_result, 'to_json'):
            return raw_result.to_json()
        if hasattr(raw_result, 'to_dict'):
            return json.dumps(raw_result.to_dict())
        if is_dataclass(raw_result):
            from dataclasses import asdict
            return json.dumps(asdict(raw_result))
        return str(raw_result)

    def serve(self):
        global current_action
        send_message(ON_STARTUP_SUCCESS, '')

        while True:
            raw_input = sys.stdin.readline()
            if not raw_input:
                break

            try:
                raw_json = base64.b64decode(raw_input.strip()).decode('utf-8')
                request = ActionRequest.from_json(raw_json)
                current_action = ActionContext(request)
                result = self.__execute(request)
                send_message(ON_ACTION_SUCCESS, result)
            except Exception as e:
                send_message(ON_ACTION_ERROR, str(e))
            finally:
                current_action = None


actions = ActionRouter()
current_action: ActionContext | None = None


def action() -> ActionContext:
    if current_action is None:
        raise RuntimeError("Nenhuma ação está em execução no momento.")
    return current_action


def send_message(prefix: str, message: str):
    payload = base64.b64encode(message.encode('utf-8')).decode('utf-8')
    print(f"{prefix}{payload}", flush=True)
