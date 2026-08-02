from typing import Any, TypeVar
from dataclasses import is_dataclass
import json
import urllib.request
import urllib.error
from ulid import ULID
from ._client import AmenoException, origin_url
from ._action_dtos import ActionRequest

T = TypeVar('T')


async def request(route: str, payload: Any, response_type: type[T]) -> T:
    payload_str = _serialize_payload(payload)
    req_dto = ActionRequest(
        id=str(ULID()),
        route=route,
        payload=payload_str,
    )
    url = f"{origin_url.get()}/api/request"
    res_data = _post_json(url, req_dto.to_dict())

    if not res_data.get('success', False):
        error_msg = res_data.get('errorMessage') or 'Erro desconhecido ao executar ação.'
        raise AmenoException(error_msg)

    response_val = res_data.get('response') or ''
    return _parse_response(response_val, response_type)


async def queue(route: str, payload: Any = '') -> None:
    payload_str = _serialize_payload(payload)
    req_dto = ActionRequest(
        id=str(ULID()),
        route=route,
        payload=payload_str,
    )
    url = f"{origin_url.get()}/api/queue"
    _post_json(url, req_dto.to_dict())


def _serialize_payload(payload: Any) -> str:
    if isinstance(payload, str):
        return payload
    if hasattr(payload, 'to_json'):
        return payload.to_json()
    if hasattr(payload, 'to_dict'):
        return json.dumps(payload.to_dict())
    return json.dumps(payload)


def _parse_response(raw_response: str, response_type: type[T]) -> T:
    if response_type == str:
        return raw_response
    if hasattr(response_type, 'from_json'):
        return response_type.from_json(raw_response)
    if hasattr(response_type, 'from_dict'):
        return response_type.from_dict(json.loads(raw_response))
    if is_dataclass(response_type):
        return response_type(**json.loads(raw_response))
    return response_type(raw_response)


def _post_json(url: str, data: dict) -> dict:
    json_bytes = json.dumps(data).encode('utf-8')
    req = urllib.request.Request(
        url=url,
        data=json_bytes,
        headers={'Content-Type': 'application/json'},
        method='POST',
    )
    try:
        with urllib.request.urlopen(req) as resp:
            if resp.status != 200:
                raise AmenoException(f'Status HTTP inesperado: {resp.status}')
            body = resp.read().decode('utf-8')
            return json.loads(body) if body else {}
    except urllib.error.HTTPError as e:
        raise AmenoException(f'Erro HTTP {e.code}: {e.reason}')
    except urllib.error.URLError as e:
        raise AmenoException(f'Erro de conexão com o AmenoLink: {e.reason}')
    except Exception as e:
        if isinstance(e, AmenoException):
            raise e
        raise AmenoException(f'Falha na requisição: {str(e)}')
