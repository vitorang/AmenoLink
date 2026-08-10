import threading
from enum import Enum
from typing import Any, Callable
from urllib.parse import quote
from signalrcore.hub_connection_builder import HubConnectionBuilder
from ._shared import client_setup, AmenoException
from ._topic_manager import TopicManager
from .dtos import TopicMessage


class ConnectionStatus(str, Enum):
    Disconnected = 'Disconnected'
    Connecting = 'Connecting'
    Connected = 'Connected'


class ConnectionManager:
    def __init__(self) -> None:
        self._connection = None
        self.status: ConnectionStatus = ConnectionStatus.Disconnected
        self._connected_event = threading.Event()
        self.topic_manager = TopicManager(self)
        self._status_listeners: set[Callable[[ConnectionStatus], None]] = set()

    @property
    def is_connected(self) -> bool:
        return self.status == ConnectionStatus.Connected

    def connect(
        self,
        on_status_change: Callable[[ConnectionStatus], None] | None = None,
        max_attempts: int = 5,
        timeout_seconds: float = 5.0,
    ) -> None:
        if on_status_change is not None:
            self._status_listeners.add(on_status_change)

        if self._connection is not None:
            if self.status != ConnectionStatus.Connected:
                self._connected_event.wait(timeout=timeout_seconds)
            return

        self._connected_event.clear()
        self._update_status(ConnectionStatus.Connecting)

        url = f'{client_setup.origin_url}/app-hub'
        if client_setup.app_name:
            url = f'{url}?appName={quote(client_setup.app_name)}'

        builder = HubConnectionBuilder().with_url(
            url
        ).with_automatic_reconnect({
            'type': 'raw',
            'keep_alive_interval': 10,
            'reconnect_interval': 5,
            'max_attempts': max_attempts,
        })

        self._connection = builder.build()
        self._connection.on_open(self._on_connection_opened)
        self._connection.on_reconnect(self._on_connection_reconnected)
        self._connection.on_close(self._on_connection_closed)
        self._connection.on('Topic.Message', self._on_topic_message_received)

        self._connection.start()
        self._connected_event.wait(timeout=timeout_seconds)

    def _on_connection_opened(self) -> None:
        self._update_status(ConnectionStatus.Connected)
        self._connected_event.set()
        self.topic_manager.resubscribe_all()

    def _on_connection_reconnected(self) -> None:
        self._update_status(ConnectionStatus.Connected)
        self.topic_manager.resubscribe_all()

    def _on_connection_closed(self) -> None:
        self._update_status(ConnectionStatus.Disconnected)

    def _update_status(self, new_status: ConnectionStatus) -> None:
        self.status = new_status
        for listener in list(self._status_listeners):
            listener(new_status)

    def send(self, method: str, arguments: list[Any]) -> None:
        if not self._connection or not self.is_connected:
            raise AmenoException(f"Não é possível enviar '{method}': cliente desconectado.")

        done_event = threading.Event()
        self._connection.send(method, arguments, lambda _: done_event.set())
        
        if not done_event.wait(timeout=3.0):
            raise AmenoException(f"Timeout ao aguardar resposta de confirmação para '{method}'.")

    def _on_topic_message_received(self, arguments: list[Any]) -> None:
        if not arguments or len(arguments) < 2:
            return

        topic_name = arguments[0]
        raw_message = arguments[1]

        if isinstance(raw_message, dict):
            topic_message = TopicMessage.from_dict(raw_message)
        else:
            topic_message = raw_message

        self.topic_manager.dispatch_message(topic_name, topic_message)


connection_manager = ConnectionManager()


def connect(
    on_status_change: Callable[[ConnectionStatus], None] | None = None,
    max_attempts: int = 5,
    timeout_seconds: float = 5.0,
) -> None:
    connection_manager.connect(on_status_change=on_status_change, max_attempts=max_attempts, timeout_seconds=timeout_seconds)