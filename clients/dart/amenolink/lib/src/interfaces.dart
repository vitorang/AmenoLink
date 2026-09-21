import 'dart:async';
import 'dtos.dart';

typedef ActionHandler<T, R> = FutureOr<R> Function(T input);
typedef CacheAllHandler = void Function(String key, dynamic value);
typedef CacheKeyHandler<T> = void Function(T? value);
typedef TopicHandler<T> = void Function(TopicMessage<T> message);

abstract interface class Action {
  String get name;
  Future<T> request<T>(dynamic payload);
  Future<void> queue(dynamic payload);
}

abstract interface class CacheWatcher {
  String get group;
  void all(CacheAllHandler handler);
  void key<T>(String key, CacheKeyHandler<T> handler);
  void dispose();
}

abstract interface class Cache {
  String get group;
  CacheWatcher watch();
  Future<T?> get<T>(String key);
  Future<void> set(String key, dynamic value);
  Future<T> getOrCreate<T>(String key, Future<T> Function() creator);
  Future<Map<String, dynamic>> all();
  Future<void> clear();
  Future<void> delete(String key);
}

abstract interface class Topic<T> {
  String get name;
  void subscribe(TopicHandler<T> handler);
  Future<void> publish(T? value, {Message? previous});
  void dispose();
}

abstract interface class ActionContext {
  ActionRequest get request;
  void log(String message);
}

abstract interface class ActionRouter {
  void add<T, R>({required String route, required ActionHandler<T, R> handler, T Function(dynamic json)? parseInput});
  void serve();
}
