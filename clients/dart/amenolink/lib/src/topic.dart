import 'dart:async';
import 'package:ulid/ulid.dart';
import 'connection_manager.dart';
import 'dtos.dart';
import 'http_requests.dart';
import 'shared.dart';
import 'topic_manager.dart';

typedef TopicHandler<T> = void Function(TopicMessage<T> message);

class Topic<T> implements ITopic {
  @override
  final String name;
  bool _disposed = false;
  final Set<TopicHandler<T>> _handlers = {};

  Topic(this.name);

  void subscribe(TopicHandler<T> handler) {
    _ensureNotDisposed();
    _handlers.add(handler);
    connectionManager.topicManager.subscribeTopic(this);
  }

  Future<void> publish(T? value, {Message? previous}) async {
    _ensureNotDisposed();

    dynamic serializedPayload = value;
    if (value != null) {
      try {
        serializedPayload = (value as dynamic).toJson();
      } catch (_) {
        serializedPayload = value;
      }
    }

    final topicMessage = TopicMessage<dynamic>(
      id: Ulid().toString(),
      previous: previous,
      createdAt: DateTime.now().toUtc(),
      topic: name,
      payload: serializedPayload,
      appName: clientSetup.appName,
    );

    final url = '${clientSetup.originUrl}/api/topic/publish';
    await postJson(url, topicMessage.toJson());
  }

  void dispose() {
    _ensureNotDisposed();
    _disposed = true;
    _handlers.clear();
    connectionManager.topicManager.unsubscribeTopic(this);
  }

  @override
  void dispatchMessage(TopicMessage message) {
    if (_disposed) return;

    final rawPayload = message.payload;
    final parsedPayload = parseData<T>(rawPayload) as T?;

    final messageToDispatch = TopicMessage<T>(
      id: message.id,
      createdAt: message.createdAt,
      previous: message.previous,
      topic: message.topic,
      payload: parsedPayload,
      appName: message.appName,
    );

    for (final handlerFunction in List<TopicHandler<T>>.from(_handlers)) {
      handlerFunction(messageToDispatch);
    }
  }

  void _ensureNotDisposed() {
    if (_disposed) {
      throw AmenoException("O tópico '$name' já foi descartado (disposed).");
    }
  }
}

Topic<T> topic<T>(String name) => Topic<T>(name);
