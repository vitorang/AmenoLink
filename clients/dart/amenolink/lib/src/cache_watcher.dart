import 'cache_manager.dart';
import 'connection_manager.dart';
import 'shared.dart';

typedef CacheAllHandler = void Function(String key, dynamic value);
typedef CacheKeyHandler<T> = void Function(T? value);

class _KeySubscription {
  final void Function(dynamic rawValue) handler;
  _KeySubscription(this.handler);
}

class CacheWatcher implements ICacheWatcher {
  @override
  final String group;
  bool _disposed = false;
  final Set<CacheAllHandler> _allHandlers = {};
  final Map<String, Set<_KeySubscription>> _keyHandlers = {};

  CacheWatcher(this.group);

  void all(CacheAllHandler handler) {
    _ensureNotDisposed();
    _allHandlers.add(handler);
    connectionManager.cacheManager.subscribeWatcher(this);
  }

  void key<T>(String key, CacheKeyHandler<T> handler) {
    _ensureNotDisposed();
    if (!_keyHandlers.containsKey(key)) {
      _keyHandlers[key] = {};
    }

    void wrapper(dynamic rawValue) {
      final parsedValue = parseData<T>(rawValue) as T?;
      handler(parsedValue);
    }

    _keyHandlers[key]!.add(_KeySubscription(wrapper));
    connectionManager.cacheManager.subscribeWatcher(this);
  }

  void dispose() {
    _ensureNotDisposed();
    _disposed = true;
    _allHandlers.clear();
    _keyHandlers.clear();
    connectionManager.cacheManager.unsubscribeWatcher(this);
  }

  @override
  void dispatchValueChanged(String key, dynamic rawValue) {
    if (_disposed) return;

    for (final handler in List<CacheAllHandler>.from(_allHandlers)) {
      handler(key, rawValue);
    }

    final keySubscriptions = _keyHandlers[key];
    if (keySubscriptions != null) {
      for (final subscription in List<_KeySubscription>.from(keySubscriptions)) {
        subscription.handler(rawValue);
      }
    }
  }

  void _ensureNotDisposed() {
    if (_disposed) {
      throw AmenoException("O observador do grupo de cache '$group' já foi descartado (disposed).");
    }
  }
}
