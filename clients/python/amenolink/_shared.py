import types
import typing
from typing import Any, TypeVar, get_origin, get_args
from dataclasses import is_dataclass

T = TypeVar('T')


class ClientSetup:
    def __init__(self, origin_url: str = 'http://localhost:13545', app_name: str = ''):
        self.origin_url: str = origin_url.rstrip('/')
        self.app_name: str = app_name


client_setup = ClientSetup()


def setup(origin_url: str = 'http://localhost:13545', app_name: str = '') -> None:
    client_setup.origin_url = origin_url.rstrip('/')
    client_setup.app_name = app_name


class AmenoException(Exception):
    def __init__(self, message: str):
        super().__init__(message)
        self.message = message


def _parse_data(data: Any, response_type: type[T]) -> T:
    if data is None:
        return None

    origin = get_origin(response_type)
    if origin in (types.UnionType, typing.Union):
        union_arguments = [arg for arg in get_args(response_type) if arg is not type(None)]
        if union_arguments:
            response_type = union_arguments[0]
            origin = get_origin(response_type)

    actual_type = origin if origin is not None else response_type

    if isinstance(actual_type, type) and isinstance(data, actual_type):
        return data

    if actual_type in (int, float, bool, str, dict):
        return actual_type(data)

    if hasattr(actual_type, 'from_dict') and isinstance(data, dict):
        generic_args = get_args(response_type)
        if generic_args and len(generic_args) > 0:
            return actual_type.from_dict(data, generic_args[0])
        return actual_type.from_dict(data)

    if is_dataclass(actual_type) and isinstance(data, dict):
        return actual_type(**data)

    if callable(actual_type):
        return actual_type(data)
    return data
