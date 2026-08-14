import 'dart:async';
import 'package:signalr_core/signalr_core.dart';
import 'cache_manager.dart';
import 'dtos.dart';
import 'shared.dart';
import 'topic_manager.dart';

enum ConnectionStatus { disconnected, connecting, connected }

class ConnectionManager implements IConnectionManager {
  HubConnection? _connection;
  ConnectionStatus status = ConnectionStatus.disconnected;
  late final TopicManager topicManager;
  late final CacheManager cacheManager;
  final Set<void Function(ConnectionStatus)> _statusListeners = {};

  ConnectionManager() {
    topicManager = TopicManager(this);
    cacheManager = CacheManager(this);
  }

  @override
  bool get isConnected => status == ConnectionStatus.connected;

  Future<void> connect({
    void Function(ConnectionStatus status)? onStatusChange,
    int maxAttempts = 5,
    double timeoutSeconds = 5.0,
  }) async {
    if (onStatusChange != null) {
      _statusListeners.add(onStatusChange);
    }

    if (_connection != null) {
      return;
    }

    _updateStatus(ConnectionStatus.connecting);

    var url = '${clientSetup.originUrl}/app-hub';
    if (clientSetup.appName.isNotEmpty) {
      final encodedAppName = Uri.encodeComponent(clientSetup.appName);
      url = '$url?appName=$encodedAppName';
    }

    final retryDelays = List.generate(maxAttempts, (_) => 2000);

    _connection = HubConnectionBuilder().withUrl(url).withAutomaticReconnect(retryDelays).build();

    _connection!.onreconnecting((error) {
      _updateStatus(ConnectionStatus.connecting);
    });

    _connection!.onreconnected((connectionId) {
      _onConnectionReconnected();
    });

    _connection!.onclose((error) {
      _onConnectionClosed();
    });

    _connection!.on('Topic.Message', _onTopicMessageReceived);
    _connection!.on('Cache.ValueChanged', _onCacheValueChanged);

    try {
      await _connection!.start()?.timeout(Duration(milliseconds: (timeoutSeconds * 1000).toInt()));
      _onConnectionOpened();
    } catch (_) {
      _updateStatus(ConnectionStatus.disconnected);
    }
  }

  Future<void> disconnect() async {
    final conn = _connection;
    if (conn != null) {
      await conn.stop();
      _connection = null;
    }
  }

  void _onConnectionOpened() {
    _updateStatus(ConnectionStatus.connected);
    topicManager.resubscribeAll();
    cacheManager.resubscribeAll();
  }

  void _onConnectionReconnected() {
    _updateStatus(ConnectionStatus.connected);
    topicManager.resubscribeAll();
    cacheManager.resubscribeAll();
  }

  void _onConnectionClosed() {
    _updateStatus(ConnectionStatus.disconnected);
  }

  void _updateStatus(ConnectionStatus newStatus) {
    status = newStatus;
    for (final listener in List<void Function(ConnectionStatus)>.from(_statusListeners)) {
      listener(newStatus);
    }
  }

  @override
  void send(String method, List<dynamic> arguments) {
    final conn = _connection;
    if (conn == null || !isConnected) {
      throw AmenoException("Não é possível enviar '$method': cliente desconectado.");
    }
    conn.send(methodName: method, args: arguments);
  }

  @override
  void subscribeTopic(ITopic topicInstance) {
    topicManager.subscribeTopic(topicInstance);
  }

  @override
  void unsubscribeTopic(ITopic topicInstance) {
    topicManager.unsubscribeTopic(topicInstance);
  }

  void _onTopicMessageReceived(List<dynamic>? arguments) {
    if (arguments == null || arguments.length < 2) return;

    final topicName = arguments[0] as String;
    final rawMessage = arguments[1];

    TopicMessage topicMessage;
    if (rawMessage is Map<String, dynamic>) {
      topicMessage = TopicMessage.fromJson(rawMessage);
    } else if (rawMessage is TopicMessage) {
      topicMessage = rawMessage;
    } else {
      return;
    }

    topicManager.dispatchMessage(topicName, topicMessage);
  }

  void _onCacheValueChanged(List<dynamic>? arguments) {
    if (arguments == null || arguments.length < 3) return;

    final groupName = arguments[0] as String;
    final key = arguments[1] as String;
    final rawValue = arguments[2];

    cacheManager.dispatchValueChanged(groupName, key, rawValue);
  }
}

final connectionManager = ConnectionManager();

Future<void> connect({
  void Function(ConnectionStatus status)? onStatusChange,
  int maxAttempts = 5,
  double timeoutSeconds = 5.0,
}) {
  return connectionManager.connect(
    onStatusChange: onStatusChange,
    maxAttempts: maxAttempts,
    timeoutSeconds: timeoutSeconds,
  );
}

Future<void> disconnect() {
  return connectionManager.disconnect();
}
