from dataclasses import dataclass, field
from datetime import datetime, timezone
from typing import Any, Self
from ulid import ULID


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

    @classmethod
    def from_dict(cls, data: dict) -> Self:
        previous_data = data.get('previous')
        previous_message = Message.from_dict(previous_data) if isinstance(previous_data, dict) else None

        return cls(
            id=data.get('id', ''),
            previous=previous_message,
            type=data.get('type', 'Message'),
            created_at=_parse_datetime(data.get('createdAt')),
        )

    def to_dict(self) -> dict:
        previous_dict = self.previous.to_dict() if hasattr(self.previous, 'to_dict') else None
        return {
            'id': self.id,
            'previous': previous_dict,
            'type': self.type,
            'createdAt': self.created_at.isoformat() if self.created_at else '',
        }


@dataclass(frozen=True)
class ActionRequest(Message):
    route: str = ''
    payload: Any = None
    type: str = 'ActionRequest'

    @classmethod
    def from_dict(cls, data: dict) -> Self:
        base_message = super().from_dict(data)
        return cls(
            id=base_message.id,
            previous=base_message.previous,
            type=data.get('type', 'ActionRequest'),
            created_at=base_message.created_at,
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
class ActionResponse(Message):
    success: bool = False
    logs: list[str] = field(default_factory=list)
    result: Any = None
    error: ActionError | None = None
    type: str = 'ActionResponse'

    @classmethod
    def from_dict(cls, data: dict) -> Self:
        base_message = super().from_dict(data)
        error_data = data.get('error')
        error_object = ActionError.from_dict(error_data) if isinstance(error_data, dict) else None

        return cls(
            id=base_message.id,
            previous=base_message.previous,
            type=data.get('type', 'ActionResponse'),
            created_at=base_message.created_at,
            success=data.get('success', False),
            logs=data.get('logs') or [],
            result=data.get('result'),
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
class TopicMessage(Message):
    topic: str = ''
    payload: Any = None
    type: str = 'TopicMessage'

    @classmethod
    def from_dict(cls, data: dict) -> Self:
        base_message = super().from_dict(data)
        return cls(
            id=base_message.id,
            previous=base_message.previous,
            type=data.get('type', 'TopicMessage'),
            created_at=base_message.created_at,
            topic=data.get('topic', ''),
            payload=data.get('payload'),
        )

    def to_dict(self) -> dict:
        result_dictionary = super().to_dict()
        result_dictionary.update({
            'topic': self.topic,
            'payload': self.payload,
        })
        return result_dictionary
