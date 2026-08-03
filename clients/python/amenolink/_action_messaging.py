import json
from typing import Any
from ulid import ULID
from ._shared import AmenoException, T, _parse_data
from ._http_requests import origin_url, _post_json
from ._action_dtos import ActionRequest


def _serialize_payload(payload: Any) -> str:
    if isinstance(payload, str):
        return payload
    if hasattr(payload, 'to_dict'):
        return json.dumps(payload.to_dict())
    return json.dumps(payload)


def request(route: str, payload: Any, response_type: type[T]) -> T:
    payload_string = _serialize_payload(payload)
    request_dto = ActionRequest(
        id=str(ULID()),
        route=route,
        payload=payload_string,
    )
    url = f"{origin_url.get()}/api/request"
    response_data = _post_json(url, request_dto.to_dict())

    if not response_data.get('success', False):
        error_message = response_data.get('errorMessage') or 'Erro desconhecido ao executar ação.'
        raise AmenoException(error_message)

    response_value = response_data.get('response') or ''
    if response_type == str:
        return response_value

    parsed_json = json.loads(response_value) if isinstance(response_value, str) else response_value
    return _parse_data(parsed_json, response_type)


def queue(route: str, payload: Any = '') -> None:
    payload_string = _serialize_payload(payload)
    request_dto = ActionRequest(
        id=str(ULID()),
        route=route,
        payload=payload_string,
    )
    url = f"{origin_url.get()}/api/queue"
    _post_json(url, request_dto.to_dict())
