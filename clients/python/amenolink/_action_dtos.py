from dataclasses import dataclass, field
from typing import Self


@dataclass(frozen=True)
class ActionRequest:
    id: str
    route: str
    payload: str

    @classmethod
    def from_dict(cls, data: dict) -> Self:
        return cls(
            id=data.get('id', ''),
            route=data.get('route', ''),
            payload=data.get('payload', ''),
        )

    def to_dict(self) -> dict:
        return {
            'id': self.id,
            'route': self.route,
            'payload': self.payload,
        }


@dataclass(frozen=True)
class ActionResponse:
    action_request: ActionRequest
    success: bool
    id: str = ''
    logs: list[str] = field(default_factory=list)
    response: str | None = None
    error_type: str | None = None
    error_message: str | None = None

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
            'response': self.response,
            'errorType': self.error_type,
            'errorMessage': self.error_message,
        }
