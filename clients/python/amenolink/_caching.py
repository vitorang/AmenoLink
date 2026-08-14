import json
import urllib.request
import urllib.parse
import urllib.error
import inspect
from dataclasses import is_dataclass, asdict
from typing import Any, Callable
from ._shared import AmenoException, T, _parse_data, client_setup


class Cache:
    def __init__(self, group_name: str):
        self.group = group_name

    def get(self, key: str, response_type: type[T]) -> T | None:
        raw_value = self._request('GET', self._cache_url(key))
        if raw_value is None:
            return None
        return _parse_data(raw_value, response_type)

    def set(self, key: str, value: Any) -> None:
        serialized_value = value
        if hasattr(value, 'to_dict'):
            serialized_value = value.to_dict()
        elif is_dataclass(value):
            serialized_value = asdict(value)

        self._request('POST', self._cache_url(key), data=serialized_value)

    def get_or_create(self, key: str, creator: Callable[[], T]) -> T:
        raw_cached = self._request('GET', self._cache_url(key))
        if raw_cached is not None:
            signature = inspect.signature(creator)
            return_annotation = signature.return_annotation
            if return_annotation not in (inspect.Signature.empty, Any):
                return _parse_data(raw_cached, return_annotation)
            return raw_cached

        created_value = creator()
        self.set(key, created_value)
        return created_value

    def all(self) -> dict[str, Any]:
        response_data = self._request('GET', self._cache_all_url())
        if not isinstance(response_data, dict):
            raise AmenoException('Resposta inesperada da API de cache')
        return response_data

    def clear(self) -> None:
        self._request('DELETE', self._cache_all_url())

    def delete(self, key: str) -> None:
        self._request('DELETE', self._cache_url(key))

    def _cache_url(self, key: str) -> str:
        return f'{client_setup.origin_url}/api/cache?' + urllib.parse.urlencode({'groupName': self.group, 'key': key})

    def _cache_all_url(self) -> str:
        return f'{client_setup.origin_url}/api/cache/all?' + urllib.parse.urlencode({'groupName': self.group})

    def _request(self, method: str, url: str, data: Any = None) -> Any:
        try:
            headers = {}
            body = None
            if data is not None:
                headers['Content-Type'] = 'application/json'
                body = json.dumps(data).encode('utf-8')

            request_object = urllib.request.Request(url=url, data=body, headers=headers, method=method)
            with urllib.request.urlopen(request_object) as response:
                if response.status != 200:
                    raise AmenoException(f'Status HTTP inesperado: {response.status}')
                content = response.read().decode('utf-8')
                if not content:
                    return None
                return json.loads(content)
        except Exception as exception:
            if isinstance(exception, AmenoException):
                raise exception
            raise AmenoException(f'Erro na operação de cache: {str(exception)}')


def cache(group_name: str) -> Cache:
    return Cache(group_name)
