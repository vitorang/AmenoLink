import 'dtos.dart';

class ClientSetup {
  String originUrl;
  String appName;

  ClientSetup({this.originUrl = 'http://localhost:13545', this.appName = ''});
}

final clientSetup = ClientSetup();

void setup({String originUrl = 'http://localhost:13545', String appName = ''}) {
  clientSetup.originUrl = originUrl.replaceAll(RegExp(r'/+$'), '');
  clientSetup.appName = appName;
}

class AmenoException implements Exception {
  final String message;

  AmenoException(this.message);

  @override
  String toString() => 'AmenoException: $message';
}

final Map<Type, Function> _typeRegistry = {};

void registerType<T>(T Function(Map<String, dynamic> json) fromJson) {
  _typeRegistry[T] = fromJson;
  _typeRegistry[ActionResponse<T>] = (json) => ActionResponse<T>.fromJson(json);
}

dynamic parseData<T>(dynamic data) {
  if (data == null) return null;

  final parser = _typeRegistry[T];
  if (parser != null && data is Map<String, dynamic>) {
    return parser(data);
  }

  return data;
}
