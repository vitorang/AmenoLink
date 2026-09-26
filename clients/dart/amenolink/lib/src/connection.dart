import 'dart:async';
import 'connection_manager.dart';
import 'interfaces.dart' as i;

class Connection implements i.Connection {
  static const int _maxReconnectAttempts = 5;
  static const double _connectionTimeoutSeconds = 5.0;

  final Set<void Function(ConnectionStatus status)> _listeners = {};
  final ConnectionManager _connectionManager;

  Connection(this._connectionManager);

  @override
  void subscribe(void Function(ConnectionStatus status) listener) {
    _listeners.add(listener);
  }

  @override
  void unsubscribe(void Function(ConnectionStatus status) listener) {
    _listeners.remove(listener);
  }

  @override
  void unsubscribeAll() {
    _listeners.clear();
  }

  void _onConnectionChanged(ConnectionStatus status) {
    for (final listener in List<void Function(ConnectionStatus)>.from(_listeners)) {
      listener(status);
    }
  }

  @override
  Future<void> connect() {
    return _connectionManager.connect(
      onStatusChange: _onConnectionChanged,
      maxAttempts: _maxReconnectAttempts,
      timeoutSeconds: _connectionTimeoutSeconds,
    );
  }

  @override
  Future<void> disconnect() {
    return _connectionManager.disconnect();
  }
}

final connection = Connection(connectionManager);
