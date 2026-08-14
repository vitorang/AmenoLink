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

  await basicExample();
  await watcherExample();
}

Future<void> basicExample() async {
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

Future<void> watcherExample() async {
  void valueChanged(String key, dynamic value) {
    print('[$key]: $value');
  }

  void userChanged(User? user) {
    print('> User: $user');
  }

  final joe = User(name: 'Average Joe', birthDate: DateTime(2010, 7, 12));
  final jane = User(name: 'Average Jane', birthDate: DateTime(2010, 12, 7));
  final c = cache('example.cache');

  await connect();

  // Esse é o observador de alterações
  final w = c.watch();

  // Pode monitorar todas as alterações
  w.all(valueChanged);
  await c.set('total', 5);
  await c.set('checked', false);

  // Ou monitorar uma chave específica
  w.key<User>('user', userChanged);
  await c.set('user', joe);
  await c.set('user', jane);

  // Ao excluir valores, eles virão nulos
  await c.delete('user');
  await c.clear();
  await Future.delayed(const Duration(seconds: 1));

  // No fim, descarte o watcher para encerrar as inscrições
  w.dispose();
  await c.set('total', 9);
  await Future.delayed(const Duration(seconds: 1));
  await c.clear();
  await disconnect();
}
