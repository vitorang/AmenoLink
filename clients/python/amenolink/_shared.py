from typing import Any, TypeVar
from dataclasses import is_dataclass

T = TypeVar('T')


class ClientConfig:
    def __init__(self, origin_url: str = 'http://localhost:13545', app_name: str = ''):
        self.origin_url: str = origin_url.rstrip('/')
        self.app_name: str = app_name


client_config = ClientConfig()


def config_client(origin_url: str = 'http://localhost:13545', app_name: str = '') -> None:
    global client_config
    client_config = ClientConfig(origin_url=origin_url, app_name=app_name)


class AmenoException(Exception):
    def __init__(self, message: str):
        super().__init__(message)
        self.message = message


def _parse_data(data: Any, response_type: type[T]) -> T:
    if data is None or isinstance(data, response_type):
        return data

    if response_type in (int, float, bool, str, dict):
        return response_type(data)

    if hasattr(response_type, 'from_dict') and isinstance(data, dict):
        return response_type.from_dict(data)

    if is_dataclass(response_type) and isinstance(data, dict):
        return response_type(**data)

    return response_type(data)
