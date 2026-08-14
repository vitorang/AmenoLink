from __future__ import annotations
from typing import TYPE_CHECKING, Any

if TYPE_CHECKING:
    from ._connection_manager import ConnectionManager
    from ._cache_watcher import CacheWatcher


class CacheManager:
    def __init__(self, connection_manager: ConnectionManager):
        self._connection_manager: ConnectionManager = connection_manager
        self.cache_map: dict[str, set[CacheWatcher]] = {}

    def subscribe_watcher(self, watcher_instance: CacheWatcher) -> None:
        group_name = getattr(watcher_instance, 'group', '')
        if not group_name:
            return

        if group_name not in self.cache_map:
            self.cache_map[group_name] = set()

        watcher_set = self.cache_map[group_name]
        is_cache_empty = len(watcher_set) == 0
        watcher_set.add(watcher_instance)

        if is_cache_empty and self._connection_manager.is_connected:
            self._connection_manager.send('Cache.Subscribe', [group_name])

    def unsubscribe_watcher(self, watcher_instance: CacheWatcher) -> None:
        group_name = getattr(watcher_instance, 'group', '')
        if not group_name or group_name not in self.cache_map:
            return

        existing_set = self.cache_map[group_name]
        if watcher_instance in existing_set:
            existing_set.remove(watcher_instance)

        if len(existing_set) == 0:
            if self._connection_manager.is_connected:
                self._connection_manager.send('Cache.Unsubscribe', [group_name])

    def resubscribe_all(self) -> None:
        if not self._connection_manager.is_connected:
            return

        for group_name, watcher_set in self.cache_map.items():
            if len(watcher_set) > 0:
                self._connection_manager.send('Cache.Subscribe', [group_name])

    def dispatch_value_changed(self, group_name: str, key: str, value: Any) -> None:
        watcher_set = self.cache_map.get(group_name, set())
        for watcher_instance in list(watcher_set):
            if hasattr(watcher_instance, '_dispatch_value_changed'):
                watcher_instance._dispatch_value_changed(key, value)
