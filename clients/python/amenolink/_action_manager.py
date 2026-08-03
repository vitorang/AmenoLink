import inspect
from dataclasses import is_dataclass
from typing import Callable, Any
import sys
import base64
import json
from ._shared import _parse_data
from ._action_dtos import ActionRequest

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
        signature = inspect.signature(handler)
        parameters = list(signature.parameters.values())

        if not parameters or parameters[0].annotation == inspect.Parameter.empty:
            raise TypeError(f"A função da rota '{route}' deve declarar o tipo do parâmetro de entrada.")

        if signature.return_annotation == inspect.Signature.empty:
            raise TypeError(f"A função da rota '{route}' deve declarar o tipo de retorno.")

        self.routes.append((route, handler))

    def __execute(self, request: ActionRequest) -> str:
        for route_name, handler in self.routes:
            if route_name == request.route:
                signature = inspect.signature(handler)
                parameters = list(signature.parameters.values())
                parameter_type = parameters[0].annotation

                argument = _parse_data(request.payload, parameter_type)
                raw_result = handler(argument)
                return self._format_result(raw_result)

        raise ValueError(f"Rota '{request.route}' não encontrada")

    def _format_result(self, raw_result: Any) -> str:
        if hasattr(raw_result, 'to_dict'):
            return json.dumps(raw_result.to_dict())
        if is_dataclass(raw_result):
            from dataclasses import asdict
            return json.dumps(asdict(raw_result))
        return json.dumps(raw_result)

    def serve(self):
        global current_action
        send_message(ON_STARTUP_SUCCESS, '')

        while True:
            raw_input = sys.stdin.readline()
            if not raw_input:
                break

            try:
                raw_json = base64.b64decode(raw_input.strip()).decode('utf-8')
                request = ActionRequest.from_dict(json.loads(raw_json))
                current_action = ActionContext(request)
                result = self.__execute(request)
                send_message(ON_ACTION_SUCCESS, result)
            except Exception as exception:
                send_message(ON_ACTION_ERROR, str(exception))
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
