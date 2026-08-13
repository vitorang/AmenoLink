import 'shared.dart';

DateTime _parseDateTime(dynamic value) {
  if (value is DateTime) return value;
  if (value is String && value.isNotEmpty) return DateTime.parse(value).toUtc();
  return DateTime.now().toUtc();
}

class Message {
  final String id;
  final Message? previous;
  final String type;
  final DateTime createdAt;
  final String appName;

  Message({required this.id, this.previous, this.type = 'Message', required this.createdAt, this.appName = ''});

  factory Message.fromJson(Map<String, dynamic> json) {
    final previousData = json['previous'];
    final previousMessage = previousData is Map<String, dynamic> ? Message.fromJson(previousData) : null;

    return Message(
      id: json['id'] as String? ?? '',
      previous: previousMessage,
      type: json['type'] as String? ?? 'Message',
      createdAt: _parseDateTime(json['createdAt']),
      appName: json['appName'] as String? ?? '',
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'previous': previous?.toJson(),
      'type': type,
      'createdAt': createdAt.toIso8601String(),
      'appName': appName,
    };
  }
}

class ActionRequest<T> extends Message {
  final String route;
  final T? payload;

  ActionRequest({
    required super.id,
    super.previous,
    super.type = 'ActionRequest',
    required super.createdAt,
    super.appName = '',
    this.route = '',
    this.payload,
  });

  factory ActionRequest.fromJson(Map<String, dynamic> json) {
    final baseMessage = Message.fromJson(json);
    final rawPayload = json['payload'];
    final parsedPayload = parseData<T>(rawPayload);

    return ActionRequest<T>(
      id: baseMessage.id,
      previous: baseMessage.previous,
      type: baseMessage.type,
      createdAt: baseMessage.createdAt,
      appName: baseMessage.appName,
      route: json['route'] as String? ?? '',
      payload: parsedPayload,
    );
  }

  @override
  Map<String, dynamic> toJson() {
    final resultDictionary = super.toJson();
    resultDictionary['route'] = route;
    resultDictionary['payload'] = payload;
    return resultDictionary;
  }
}

class ActionError {
  final String type;
  final String message;

  ActionError({this.type = '', this.message = ''});

  factory ActionError.fromJson(Map<String, dynamic> json) {
    return ActionError(type: json['type'] as String? ?? '', message: json['message'] as String? ?? '');
  }

  Map<String, dynamic> toJson() {
    return {'type': type, 'message': message};
  }
}

class ActionResponse<T> extends Message {
  final bool success;
  final List<String> logs;
  final T? result;
  final ActionError? error;

  ActionResponse({
    required super.id,
    super.previous,
    super.type = 'ActionResponse',
    required super.createdAt,
    super.appName = '',
    this.success = false,
    this.logs = const [],
    this.result,
    this.error,
  });

  factory ActionResponse.fromJson(Map<String, dynamic> json) {
    final baseMessage = Message.fromJson(json);
    final errorData = json['error'];
    final errorObject = errorData is Map<String, dynamic> ? ActionError.fromJson(errorData) : null;

    final rawResult = json['result'];
    final parsedResult = parseData<T>(rawResult);

    final rawLogs = json['logs'] as List<dynamic>?;
    final logsList = rawLogs?.map((e) => e.toString()).toList() ?? [];

    return ActionResponse<T>(
      id: baseMessage.id,
      previous: baseMessage.previous,
      type: baseMessage.type,
      createdAt: baseMessage.createdAt,
      appName: baseMessage.appName,
      success: json['success'] as bool? ?? false,
      logs: logsList,
      result: parsedResult,
      error: errorObject,
    );
  }

  @override
  Map<String, dynamic> toJson() {
    final resultDictionary = super.toJson();
    resultDictionary['success'] = success;
    resultDictionary['logs'] = logs;
    resultDictionary['result'] = result;
    resultDictionary['error'] = error?.toJson();
    return resultDictionary;
  }
}

class TopicMessage<T> extends Message {
  final String topic;
  final T? payload;

  TopicMessage({
    required super.id,
    super.previous,
    super.type = 'TopicMessage',
    required super.createdAt,
    super.appName = '',
    this.topic = '',
    this.payload,
  });

  factory TopicMessage.fromJson(Map<String, dynamic> json) {
    final baseMessage = Message.fromJson(json);
    final rawPayload = json['payload'];
    final parsedPayload = parseData<T>(rawPayload);

    return TopicMessage<T>(
      id: baseMessage.id,
      previous: baseMessage.previous,
      type: baseMessage.type,
      createdAt: baseMessage.createdAt,
      appName: baseMessage.appName,
      topic: json['topic'] as String? ?? '',
      payload: parsedPayload,
    );
  }

  @override
  Map<String, dynamic> toJson() {
    final resultDictionary = super.toJson();
    dynamic serializedPayload = payload;
    if (payload != null) {
      try {
        serializedPayload = (payload as dynamic).toJson();
      } catch (_) {
        serializedPayload = payload;
      }
    }

    resultDictionary['topic'] = topic;
    resultDictionary['payload'] = serializedPayload;
    return resultDictionary;
  }
}
