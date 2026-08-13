import 'dart:convert';
import 'package:http/http.dart' as http;

Future<Map<String, dynamic>> postJson(String url, Map<String, dynamic> data) async {
  final response = await http.post(
    Uri.parse(url),
    headers: {'Content-Type': 'application/json'},
    body: jsonEncode(data),
  );

  if (response.body.trim().isEmpty) {
    return {};
  }

  return jsonDecode(response.body) as Map<String, dynamic>;
}
