from typing import Callable
from ._connection_manager import connection_manager, ConnectionStatus

type ConnectionStatusListener = Callable[[ConnectionStatus], None]

MAX_RECONNECT_ATTEMPTS = 5
CONNECTION_TIMEOUT_SECONDS = 5.0


class Connection:
    def __init__(self) -> None:
        self._listeners: set[ConnectionStatusListener] = set()

    def subscribe(self, listener: ConnectionStatusListener) -> None:
        self._listeners.add(listener)

    def unsubscribe(self, listener: ConnectionStatusListener) -> None:
        self._listeners.discard(listener)

    def unsubscribe_all(self) -> None:
        self._listeners.clear()

    def _on_connection_changed(self, status: ConnectionStatus) -> None:
        for listener in list(self._listeners):
            listener(status)

    def connect(self) -> None:
        connection_manager.connect(
            on_status_change=self._on_connection_changed,
            max_attempts=MAX_RECONNECT_ATTEMPTS,
            timeout_seconds=CONNECTION_TIMEOUT_SECONDS,
        )

    def disconnect(self) -> None:
        connection_manager.disconnect()


connection = Connection()
