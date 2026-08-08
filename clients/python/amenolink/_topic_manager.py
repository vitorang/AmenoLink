from __future__ import annotations
from typing import TYPE_CHECKING
from .dtos import TopicMessage

if TYPE_CHECKING:
    from ._connection_manager import ConnectionManager
    from ._topic import Topic


class TopicManager:
    def __init__(self, connection_manager: ConnectionManager):
        self._connection_manager: ConnectionManager = connection_manager
        self.topic_map: dict[str, set[Topic]] = {}

    def subscribe_topic(self, topic_instance: Topic) -> None:
        topic_name = getattr(topic_instance, 'name', '')
        if not topic_name:
            return

        if topic_name not in self.topic_map:
            self.topic_map[topic_name] = set()

        topic_set = self.topic_map[topic_name]
        is_topic_empty = len(topic_set) == 0
        topic_set.add(topic_instance)

        if is_topic_empty and self._connection_manager.is_connected:
            self._connection_manager.send('Topic.Subscribe', [topic_name])

    def unsubscribe_topic(self, topic_instance: Topic) -> None:
        topic_name = getattr(topic_instance, 'name', '')
        if not topic_name or topic_name not in self.topic_map:
            return

        existing_set = self.topic_map[topic_name]
        if topic_instance in existing_set:
            existing_set.remove(topic_instance)

        if len(existing_set) == 0:
            if self._connection_manager.is_connected:
                self._connection_manager.send('Topic.Unsubscribe', [topic_name])

    def resubscribe_all(self) -> None:
        if not self._connection_manager.is_connected:
            return

        for topic_name, topic_set in self.topic_map.items():
            if len(topic_set) > 0:
                self._connection_manager.send('Topic.Subscribe', [topic_name])

    def dispatch_message(self, topic_name: str, topic_message: TopicMessage) -> None:
        topic_set = self.topic_map.get(topic_name, set())
        for topic_instance in list(topic_set):
            if hasattr(topic_instance, '_dispatch_message'):
                topic_instance._dispatch_message(topic_message)