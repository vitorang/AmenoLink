import 'dart:async';
import 'package:ulid/ulid.dart';
import 'dtos.dart';
import 'http_requests.dart';
import 'shared.dart';

Future<T> request<T>(String route, dynamic payload) async {
  dynamic serializedPayload = payload;
  if (payload != null) {
    try {
      serializedPayload = (payload as dynamic).toJson();
    } catch (_) {
      serializedPayload = payload;
    }
  }

  final requestDto = ActionRequest(
    id: Ulid().toString(),
    createdAt: DateTime.now().toUtc(),
    route: route,
    payload: serializedPayload,
    appName: clientSetup.appName,
  );

  final url = '${clientSetup.originUrl}/api/request';
  final responseData = await postJson(url, requestDto.toJson());

  if (responseData['success'] != true) {
    final errorInfo = responseData['error'];
    String? errorMessage;
    if (errorInfo is Map<String, dynamic>) {
      errorMessage = errorInfo['message'] as String?;
    }
    if (errorMessage == null || errorMessage.isEmpty) {
      errorMessage = responseData['errorMessage'] as String? ?? 'Erro desconhecido ao executar ação.';
    }
    throw AmenoException(errorMessage);
  }

  final responseValue = responseData.containsKey('result') ? responseData['result'] : responseData['response'];
  return parseData<T>(responseValue) as T;
}

Future<void> queue(String route, dynamic payload) async {
  dynamic serializedPayload = payload;
  if (payload != null) {
    try {
      serializedPayload = (payload as dynamic).toJson();
    } catch (_) {
      serializedPayload = payload;
    }
  }

  final requestDto = ActionRequest(
    id: Ulid().toString(),
    createdAt: DateTime.now().toUtc(),
    route: route,
    payload: serializedPayload,
    appName: clientSetup.appName,
  );

  final url = '${clientSetup.originUrl}/api/queue';
  await postJson(url, requestDto.toJson());
}
