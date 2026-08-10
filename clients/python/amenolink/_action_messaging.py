from dataclasses import is_dataclass
from typing import Any
from ulid import ULID
from ._shared import AmenoException, T, _parse_data, client_setup
from ._http_requests import _post_json
from .dtos import ActionRequest


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
        app_name=client_setup.app_name,
    )
    url = f'{client_setup.origin_url}/api/request'
    response_data = _post_json(url, request_dto.to_dict())

    if not response_data.get('success', False):
        error_info = response_data.get('error')
        error_message = None
        if isinstance(error_info, dict):
            error_message = error_info.get('message')
        if not error_message:
            error_message = response_data.get('errorMessage') or 'Erro desconhecido ao executar ação.'
        raise AmenoException(error_message)

    response_value = response_data.get('result') if 'result' in response_data else response_data.get('response')
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
        app_name=client_setup.app_name,
    )
    url = f'{client_setup.origin_url}/api/queue'
    _post_json(url, request_dto.to_dict())
