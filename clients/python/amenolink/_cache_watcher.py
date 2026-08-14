import inspect
from dataclasses import dataclass
from typing import Any, Callable, Generic
from ._shared import AmenoException, T, _parse_data
from ._connection_manager import connection_manager


@dataclass(frozen=True)
class _KeySubscription(Generic[T]):
    value_type: type[T]
    handler: Callable[[T | None], None]


class CacheWatcher:
    def __init__(self, group: str):
        self.group: str = group
        self._disposed: bool = False
        self._all_handlers: set[Callable[[str, Any], None]] = set()
        self._key_handlers: dict[str, set[_KeySubscription[Any]]] = {}

    def all(self, handler: Callable[[str, Any], None]) -> None:
        self._ensure_not_disposed()
        self._all_handlers.add(handler)
        connection_manager.cache_manager.subscribe_watcher(self)

    def key(self, key: str, handler: Callable[[T | None], None]) -> None:
        self._ensure_not_disposed()
        if key not in self._key_handlers:
            self._key_handlers[key] = set()

        resolved_type = Any
        signature = inspect.signature(handler)
        parameters = list(signature.parameters.values())
        if parameters and parameters[0].annotation not in (inspect.Parameter.empty, Any):
            resolved_type = parameters[0].annotation

        subscription = _KeySubscription(value_type=resolved_type, handler=handler)
        self._key_handlers[key].add(subscription)
        connection_manager.cache_manager.subscribe_watcher(self)

    def dispose(self) -> None:
        self._ensure_not_disposed()
        self._disposed = True
        self._all_handlers.clear()
        self._key_handlers.clear()
        connection_manager.cache_manager.unsubscribe_watcher(self)

    def _dispatch_value_changed(self, key: str, raw_value: Any) -> None:
        if self._disposed:
            return

        for handler_function in list(self._all_handlers):
            handler_function(key, raw_value)

        subscriptions = list(self._key_handlers.get(key, set()))
        for subscription in subscriptions:
            parsed_value = raw_value
            if subscription.value_type is not Any and raw_value is not None:
                parsed_value = _parse_data(raw_value, subscription.value_type)
            subscription.handler(parsed_value)

    def _ensure_not_disposed(self) -> None:
        if self._disposed:
            raise AmenoException(f"O observador do grupo de cache '{self.group}' já foi descartado (disposed).")
