import 'topic_manager.dart';

abstract class ICacheWatcher {
  String get group;
  void dispatchValueChanged(String key, dynamic value);
}

class CacheManager {
  final IConnectionManager _connectionManager;
  final Map<String, Set<ICacheWatcher>> cacheMap = {};

  CacheManager(this._connectionManager);

  void subscribeWatcher(ICacheWatcher watcherInstance) {
    final groupName = watcherInstance.group;
    if (groupName.isEmpty) return;

    if (!cacheMap.containsKey(groupName)) {
      cacheMap[groupName] = {};
    }

    final watcherSet = cacheMap[groupName]!;
    final isCacheEmpty = watcherSet.isEmpty;
    watcherSet.add(watcherInstance);

    if (isCacheEmpty && _connectionManager.isConnected) {
      _connectionManager.send('Cache.Subscribe', [groupName]);
    }
  }

  void unsubscribeWatcher(ICacheWatcher watcherInstance) {
    final groupName = watcherInstance.group;
    if (groupName.isEmpty || !cacheMap.containsKey(groupName)) return;

    final existingSet = cacheMap[groupName]!;
    if (existingSet.contains(watcherInstance)) {
      existingSet.remove(watcherInstance);
    }

    if (existingSet.isEmpty) {
      if (_connectionManager.isConnected) {
        _connectionManager.send('Cache.Unsubscribe', [groupName]);
      }
    }
  }

  void resubscribeAll() {
    if (!_connectionManager.isConnected) return;

    for (final entry in cacheMap.entries) {
      if (entry.value.isNotEmpty) {
        _connectionManager.send('Cache.Subscribe', [entry.key]);
      }
    }
  }

  void dispatchValueChanged(String groupName, String key, dynamic value) {
    final watcherSet = cacheMap[groupName] ?? {};
    for (final watcherInstance in List<ICacheWatcher>.from(watcherSet)) {
      watcherInstance.dispatchValueChanged(key, value);
    }
  }
}
