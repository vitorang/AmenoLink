/*
    Exemplos de execução de Cache. Será demonstrado como manipular valores e grupos.

    CONFIGURAÇÃO
    - Execute o programa AmenoLink, vá na aba CACHES e adicione "example.cache" (sem aspas).
*/

import 'package:amenolink/amenolink.dart';
import 'dtos.dart';

const cacheGroup = 'example.cache';

void main() async {
  setup(appName: 'Cache Example (Dart)');
  registerType<User>(User.fromJson);

  // Declaração do grupo de cache
  final exampleCache = cache(cacheGroup);

  // Valida se os recursos estão configurados
  await ensureReady();

  await basicExample(exampleCache);
  await watcherExample(exampleCache);
}

Future<void> basicExample(Cache exampleCache) async {
  final garyStu = User(name: 'Gary Stu', birthDate: DateTime(2001, 1, 20));
  final marySue = User(name: 'Mary Sue', birthDate: DateTime(1988, 8, 19));

  // Valor não definido retorna null
  var user = await exampleCache.get<User>('gary');
  print('get: $user\n');

  // Caso não exista, será criado
  user = await exampleCache.getOrCreate<User>('gary', () async => garyStu);
  print('getOrCreate: $user');
  print('get: $user\n');

  // Definir e excluir valores
  await exampleCache.set('mary', marySue);
  user = await exampleCache.get<User>('mary');
  print('set: $user\n');

  await exampleCache.delete('mary');
  user = await exampleCache.get<User>('mary');
  print('delete: $user\n');

  // Obter todos os registros:
  await exampleCache.set('mary', marySue);
  await exampleCache.set('port', 13545);
  await exampleCache.set('true', true);
  var entries = await exampleCache.all();
  print('all: $entries\n');

  // Remover todos os registros:
  await exampleCache.clear();
  entries = await exampleCache.all();
  print('clear: $entries\n');
}

Future<void> watcherExample(Cache exampleCache) async {
  void valueChanged(String key, dynamic value) {
    print('[$key]: $value');
  }

  void userChanged(User? user) {
    print('> User: $user');
  }

  final joe = User(name: 'Average Joe', birthDate: DateTime(2010, 7, 12));
  final jane = User(name: 'Average Jane', birthDate: DateTime(2010, 12, 7));

  await connect();

  // Esse é o observador de alterações
  final watcher = exampleCache.watch();

  // Pode monitorar todas as alterações
  watcher.all(valueChanged);
  await exampleCache.set('total', 5);
  await exampleCache.set('checked', false);

  // Ou monitorar uma chave específica
  watcher.key<User>('user', userChanged);
  await exampleCache.set('user', joe);
  await exampleCache.set('user', jane);

  // Ao excluir valores, eles virão nulos
  await exampleCache.delete('user');
  await exampleCache.clear();
  await Future.delayed(const Duration(seconds: 1));

  // No fim, descarte o watcher para encerrar as inscrições
  watcher.dispose();
  await exampleCache.set('total', 9);
  await Future.delayed(const Duration(seconds: 1));
  await exampleCache.clear();
  await disconnect();
}
