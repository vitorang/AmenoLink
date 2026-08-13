import 'dart:convert';
import 'package:http/http.dart' as http;
import 'shared.dart';

class Cache {
  final String group;

  Cache(this.group);

  Future<T?> get<T>(String key) async {
    final rawValue = await _request('GET', _cacheUrl(key));
    if (rawValue == null) return null;
    return parseData<T>(rawValue) as T?;
  }

  Future<void> set(String key, dynamic value) async {
    dynamic serializedValue = value;
    try {
      serializedValue = (value as dynamic).toJson();
    } catch (_) {
      serializedValue = value;
    }

    await _request('POST', _cacheUrl(key), data: serializedValue);
  }

  Future<T> getOrCreate<T>(String key, Future<T> Function() creator) async {
    final cachedValue = await get<T>(key);
    if (cachedValue != null) return cachedValue;

    final createdValue = await creator();
    await set(key, createdValue);
    return createdValue;
  }

  Future<Map<String, dynamic>> all() async {
    final responseData = await _request('GET', _cacheAllUrl());
    if (responseData is! Map<String, dynamic>) {
      throw AmenoException('Resposta inesperada da API de cache');
    }
    return responseData;
  }

  Future<void> clear() async {
    await _request('DELETE', _cacheAllUrl());
  }

  Future<void> delete(String key) async {
    await _request('DELETE', _cacheUrl(key));
  }

  String _cacheUrl(String key) {
    final uri = Uri.parse(
      '${clientSetup.originUrl}/api/cache',
    ).replace(queryParameters: {'groupName': group, 'key': key});
    return uri.toString();
  }

  String _cacheAllUrl() {
    final uri = Uri.parse('${clientSetup.originUrl}/api/cache/all').replace(queryParameters: {'groupName': group});
    return uri.toString();
  }

  Future<dynamic> _request(String method, String url, {dynamic data}) async {
    try {
      final headers = <String, String>{};
      String? body;
      if (data != null) {
        headers['Content-Type'] = 'application/json';
        body = jsonEncode(data);
      }

      final uri = Uri.parse(url);
      http.Response response;

      switch (method.toUpperCase()) {
        case 'GET':
          response = await http.get(uri, headers: headers);
          break;
        case 'POST':
          response = await http.post(uri, headers: headers, body: body);
          break;
        case 'DELETE':
          response = await http.delete(uri, headers: headers, body: body);
          break;
        default:
          throw AmenoException('Método HTTP não suportado: $method');
      }

      if (response.statusCode != 200) {
        throw AmenoException('Status HTTP inesperado: ${response.statusCode}');
      }
      if (response.body.isEmpty) return null;
      return jsonDecode(response.body);
    } catch (exception) {
      if (exception is AmenoException) rethrow;
      throw AmenoException('Erro na operação de cache: $exception');
    }
  }
}

Cache cache(String groupName) => Cache(groupName);
