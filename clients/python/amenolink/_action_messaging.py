from dataclasses import is_dataclass
from typing import Any
from ulid import ULID
from ._shared import AmenoException, T, _parse_data
from ._http_requests import origin_url, _post_json
from ._action_dtos import ActionRequest


def request(route: str, payload: Any, response_type: type[T]) -> T:
    if hasattr(payload, 'to_dict'):
        payload = payload.to_dict()
    elif is_dataclass(payload):
        from dataclasses import asdict
        payload = asdict(payload)

    request_dto = ActionRequest(
        id=str(ULID()),
        route=route,
        payload=payload,
    )
    url = f"{origin_url.get()}/api/request"
    response_data = _post_json(url, request_dto.to_dict())

    if not response_data.get('success', False):
        error_message = response_data.get('errorMessage') or 'Erro desconhecido ao executar ação.'
        raise AmenoException(error_message)

    response_value = response_data.get('response')
    return _parse_data(response_value, response_type)


def queue(route: str, payload: Any = None) -> None:
    if hasattr(payload, 'to_dict'):
        payload = payload.to_dict()
    elif is_dataclass(payload):
        from dataclasses import asdict
        payload = asdict(payload)

    request_dto = ActionRequest(
        id=str(ULID()),
        route=route,
        payload=payload,
    )
    url = f"{origin_url.get()}/api/queue"
    _post_json(url, request_dto.to_dict())
