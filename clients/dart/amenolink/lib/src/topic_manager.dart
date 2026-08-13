import 'dtos.dart';

abstract class IConnectionManager {
  bool get isConnected;
  void send(String method, List<dynamic> arguments);
  void subscribeTopic(ITopic topicInstance);
  void unsubscribeTopic(ITopic topicInstance);
}

abstract class ITopic {
  String get name;
  void dispatchMessage(TopicMessage message);
}

class TopicManager {
  final IConnectionManager _connectionManager;
  final Map<String, Set<ITopic>> topicMap = {};

  TopicManager(this._connectionManager);

  void subscribeTopic(ITopic topicInstance) {
    final topicName = topicInstance.name;
    if (topicName.isEmpty) return;

    if (!topicMap.containsKey(topicName)) {
      topicMap[topicName] = {};
    }

    final topicSet = topicMap[topicName]!;
    final isTopicEmpty = topicSet.isEmpty;
    topicSet.add(topicInstance);

    if (isTopicEmpty && _connectionManager.isConnected) {
      _connectionManager.send('Topic.Subscribe', [topicName]);
    }
  }

  void unsubscribeTopic(ITopic topicInstance) {
    final topicName = topicInstance.name;
    if (topicName.isEmpty || !topicMap.containsKey(topicName)) return;

    final existingSet = topicMap[topicName]!;
    if (existingSet.contains(topicInstance)) {
      existingSet.remove(topicInstance);
    }

    if (existingSet.isEmpty) {
      if (_connectionManager.isConnected) {
        _connectionManager.send('Topic.Unsubscribe', [topicName]);
      }
    }
  }

  void resubscribeAll() {
    if (!_connectionManager.isConnected) return;

    for (final entry in topicMap.entries) {
      if (entry.value.isNotEmpty) {
        _connectionManager.send('Topic.Subscribe', [entry.key]);
      }
    }
  }

  void dispatchMessage(String topicName, TopicMessage topicMessage) {
    final topicSet = topicMap[topicName] ?? {};
    for (final topicInstance in List<ITopic>.from(topicSet)) {
      topicInstance.dispatchMessage(topicMessage);
    }
  }
}
