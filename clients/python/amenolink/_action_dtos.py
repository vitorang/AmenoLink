from dataclasses import dataclass, field
from typing import Any, Self


@dataclass(frozen=True)
class ActionRequest:
    id: str
    route: str
    payload: Any = None

    @classmethod
    def from_dict(cls, data: dict) -> Self:
        return cls(
            id=data.get('id', ''),
            route=data.get('route', ''),
            payload=data.get('payload'),
        )

    def to_dict(self) -> dict:
        return {
            'id': self.id,
            'route': self.route,
            'payload': self.payload,
        }


@dataclass(frozen=True)
class ActionError:
    type: str
    message: str

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
class ActionResponse:
    action_request: ActionRequest
    success: bool
    id: str = ''
    logs: list[str] = field(default_factory=list)
    result: Any = None
    error: ActionError | None = None

    def to_dict(self) -> dict:
        return {
            'actionRequest': {
                'id': self.action_request.id,
                'route': self.action_request.route,
                'payload': self.action_request.payload,
            },
            'success': self.success,
            'id': self.id,
            'logs': self.logs,
            'result': self.result,
            'error': self.error.to_dict() if self.error else None,
        }
