from dataclasses import is_dataclass, asdict
from typing import Any, Callable, Generic
from ulid import ULID
from ._shared import AmenoException, T, _parse_data, client_config
from ._http_requests import _post_json
from ._connection_manager import connection_manager
from .dtos import Message, TopicMessage


class Topic(Generic[T]):
    def __init__(self, name: str, value_type: type[T] = Any):
        self.name: str = name
        self.value_type: type[T] = value_type
        self.disposed: bool = False
        self._handlers: set[Callable[[T | None, TopicMessage], None]] = set()

    def subscribe(self, handler: Callable[[T | None, TopicMessage], None]) -> None:
        self._ensure_not_disposed()
        self._handlers.add(handler)
        connection_manager.topic_manager.subscribe_topic(self)

    def publish(self, value: T | None = None, previous: Message | None = None) -> None:
        self._ensure_not_disposed()

        serialized_payload = value
        if hasattr(value, 'to_dict'):
            serialized_payload = value.to_dict()
        elif is_dataclass(value):
            serialized_payload = asdict(value)

        topic_message = TopicMessage(
            id=str(ULID()),
            previous=previous,
            topic=self.name,
            payload=serialized_payload,
            app_name=client_config.app_name,
        )

        url = f'{client_config.origin_url}/api/topic/publish'
        _post_json(url, topic_message.to_dict())

    def dispose(self) -> None:
        self._ensure_not_disposed()
        self.disposed = True
        self._handlers.clear()
        connection_manager.topic_manager.unsubscribe_topic(self)

    def _dispatch_message(self, message: TopicMessage) -> None:
        if self.disposed:
            return

        parsed_value = message.payload
        if self.value_type is not Any and message.payload is not None:
            parsed_value = _parse_data(message.payload, self.value_type)

        for handler_function in list(self._handlers):
            handler_function(parsed_value, message)

    def _ensure_not_disposed(self) -> None:
        if self.disposed:
            raise AmenoException(f"O tópico '{self.name}' já foi descartado (disposed).")


def topic(name: str, value_type: type[T] = Any) -> Topic[T]:
    return Topic(name=name, value_type=value_type)
