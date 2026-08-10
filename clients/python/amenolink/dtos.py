from __future__ import annotations
from dataclasses import dataclass, field
from datetime import datetime, timezone
from typing import Any, Generic, TypeVar, Self
from ulid import ULID

T = TypeVar('T')


def _parse_datetime(value: Any) -> datetime:
    if isinstance(value, datetime):
        return value
    if isinstance(value, str) and value:
        return datetime.fromisoformat(value.replace('Z', '+00:00'))
    return datetime.now(timezone.utc)


@dataclass(frozen=True)
class Message:
    id: str = field(default_factory=lambda: str(ULID()))
    previous: Any = None
    type: str = 'Message'
    created_at: datetime = field(default_factory=lambda: datetime.now(timezone.utc))
    app_name: str = ''

    @classmethod
    def from_dict(cls, data: dict) -> Self:
        previous_data = data.get('previous')
        previous_message = Message.from_dict(previous_data) if isinstance(previous_data, dict) else None

        return cls(
            id=data.get('id', ''),
            previous=previous_message,
            type=data.get('type', 'Message'),
            created_at=_parse_datetime(data.get('createdAt')),
            app_name=data.get('appName', ''),
        )

    def to_dict(self) -> dict:
        previous_dict = self.previous.to_dict() if hasattr(self.previous, 'to_dict') else None
        return {
            'id': self.id,
            'previous': previous_dict,
            'type': self.type,
            'createdAt': self.created_at.isoformat() if self.created_at else '',
            'appName': self.app_name,
        }


@dataclass(frozen=True)
class ActionRequest(Message, Generic[T]):
    route: str = ''
    payload: T = None  # type: ignore
    type: str = 'ActionRequest'

    @classmethod
    def from_dict(cls, data: dict) -> Self:
        base_message = super().from_dict(data)
        return cls(
            id=base_message.id,
            previous=base_message.previous,
            type=data.get('type', 'ActionRequest'),
            created_at=base_message.created_at,
            app_name=base_message.app_name,
            route=data.get('route', ''),
            payload=data.get('payload'),
        )

    def to_dict(self) -> dict:
        result_dictionary = super().to_dict()
        result_dictionary.update({
            'route': self.route,
            'payload': self.payload,
        })
        return result_dictionary


@dataclass(frozen=True)
class ActionError:
    type: str = ''
    message: str = ''

    @classmethod
    def from_dict(cls, data: dict) -> Self:
        return cls(
            type=data.get('type', ''),
            message=data.get('message', ''),
        )

    def to_dict(self) -> dict:
        return {
            'type': self.type,
            'message': self.message,
        }


@dataclass(frozen=True)
class ActionResponse(Message, Generic[T]):
    success: bool = False
    logs: list[str] = field(default_factory=list)
    result: T | None = None
    error: ActionError | None = None
    type: str = 'ActionResponse'

    @classmethod
    def from_dict(cls, data: dict, item_type: type[T] | type | None = None) -> Self:
        from ._shared import _parse_data
        base_message = super().from_dict(data)
        error_data = data.get('error')
        error_object = ActionError.from_dict(error_data) if isinstance(error_data, dict) else None

        raw_result = data.get('result')
        parsed_result = raw_result
        if item_type is not None and raw_result is not None:
            parsed_result = _parse_data(raw_result, item_type)

        return cls(
            id=base_message.id,
            previous=base_message.previous,
            type=data.get('type', 'ActionResponse'),
            created_at=base_message.created_at,
            app_name=base_message.app_name,
            success=data.get('success', False),
            logs=data.get('logs') or [],
            result=parsed_result,
            error=error_object,
        )

    def to_dict(self) -> dict:
        result_dictionary = super().to_dict()
        error_dictionary = self.error.to_dict() if self.error else None
        result_dictionary.update({
            'success': self.success,
            'logs': self.logs,
            'result': self.result,
            'error': error_dictionary,
        })
        return result_dictionary


@dataclass(frozen=True)
class TopicMessage(Message, Generic[T]):
    topic: str = ''
    payload: T = None  # type: ignore
    type: str = 'TopicMessage'

    @classmethod
    def from_dict(cls, data: dict, item_type: type[T] | type | None = None) -> Self:

        from ._shared import _parse_data
        base_message = super().from_dict(data)
        raw_payload = data.get('payload')
        parsed_payload = raw_payload
        if item_type is not None and raw_payload is not None:
            parsed_payload = _parse_data(raw_payload, item_type)

        return cls(
            id=base_message.id,
            previous=base_message.previous,
            type=data.get('type', 'TopicMessage'),
            created_at=base_message.created_at,
            app_name=base_message.app_name,
            topic=data.get('topic', ''),
            payload=parsed_payload,
        )

    def to_dict(self) -> dict:
        from dataclasses import is_dataclass, asdict
        result_dictionary = super().to_dict()
        serialized_payload = self.payload
        if hasattr(self.payload, 'to_dict'):
            serialized_payload = self.payload.to_dict()
        elif is_dataclass(self.payload):
            serialized_payload = asdict(self.payload)

        result_dictionary.update({
            'topic': self.topic,
            'payload': serialized_payload,
        })
        return result_dictionary
