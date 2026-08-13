/*
    Exemplos de execução de Cache. Será demonstrado como manipular valores e grupos.

    CONFIGURAÇÃO
    - Execute o programa AmenoLink, vá na aba CACHES e adicione "example.cache" (sem aspas).
*/

import 'package:amenolink/amenolink.dart';
import 'dtos.dart';

void main() async {
  setup(appName: 'Cache Example (Dart)');
  registerType<User>(User.fromJson);

  final garyStu = User(name: 'Gary Stu', birthDate: DateTime(2001, 1, 20));
  final marySue = User(name: 'Mary Sue', birthDate: DateTime(1988, 8, 19));

  // Esse é o grupo de valores
  final c = cache('example.cache');

  // Valor não definido retorna null
  var user = await c.get<User>('gary');
  print('get: $user\n');

  // Caso não exista, será criado
  user = await c.getOrCreate<User>('gary', () async => garyStu);
  print('getOrCreate: $user');
  print('get: $user\n');

  // Definir e excluir valores
  await c.set('mary', marySue);
  user = await c.get<User>('mary');
  print('set: $user\n');

  await c.delete('mary');
  user = await c.get<User>('mary');
  print('delete: $user\n');

  // Obter todos os registros:
  await c.set('mary', marySue);
  await c.set('port', 13545);
  await c.set('true', true);
  var map = await c.all();
  print('all: $map\n');

  // Remover todos os registros:
  await c.clear();
  map = await c.all();
  print('clear: $map\n');
}
