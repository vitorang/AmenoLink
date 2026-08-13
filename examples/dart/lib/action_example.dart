/*
    Exemplos de execução de Action. Será demonstrado como usar recursos de requisição e filas.

    CONFIGURAÇÃO
    - No diretório onde está este arquivo, execute o comando para instalar as dependências Dart:
        dart pub get

    - Execute o programa AmenoLink e vá na aba PROGRAMAS. Adicione action_server.dart
        e adicione "example.action" (sem aspas) na seção Ações.

    - Na aba TÓPICOS, adicione "example.action".
*/

import 'package:amenolink/amenolink.dart';
import 'dtos.dart';

const actionRoute = 'example.action';

void main() async {
  setup(appName: 'Action Example (Dart)');

  // Registra os DTOs
  registerType<UserAstrology>(UserAstrology.fromJson);

  final garyStu = User(name: 'Gary Stu', birthDate: DateTime(2001, 1, 20));
  final marySue = User(name: 'Mary Sue', birthDate: DateTime(1988, 8, 19));

  // Abre uma conexão persistente
  await connect(onStatusChange: onStatusChange);

  // Os resultados são publicados no tópico com mesmo nome da ação
  final t = topic<ActionResponse<UserAstrology>>(actionRoute);
  t.subscribe(onMessageReceived);

  // Com request poderá obter resultados de forma síncrona
  // e não precisará usar tópico ou conexão persistente.
  // Porém, o resultado será publicado no tópico!
  final ua = await request<UserAstrology>(actionRoute, garyStu);
  print('Resposta de requisição: ${formatUserAstrology(ua)}');
  await Future.delayed(const Duration(milliseconds: 500));

  // Ou executar de forma assíncrona se não precisar do resultado ou o processamento for lento
  await queue(actionRoute, marySue);
  await Future.delayed(const Duration(seconds: 1));

  // Desativa conexão do tópico. Após isso, ele não poderá ser usado
  t.dispose();

  // Fecha a conexão. É necessário para o programa se encerrar.
  await disconnect();
}

void onStatusChange(dynamic status) {
  print('Estado da conexão: $status');
}

// Actions retornam mensagens de tópico com resposta dentro
void onMessageReceived(TopicMessage<ActionResponse<UserAstrology>> message) async {
  await Future.delayed(const Duration(milliseconds: 100));
  final response = message.payload!;
  print('Mensagem do tópico: \n\tLogs: ${response.logs}${formatUserAstrology(response.result)}');
}

String formatUserAstrology(UserAstrology? ua) {
  if (ua == null) return '';
  final day = ua.birthDate.day.toString().padLeft(2, '0');
  final month = ua.birthDate.month.toString().padLeft(2, '0');
  final year = ua.birthDate.year;
  final birthDateStr = '$day/$month/$year';

  return '\n\t${ua.name} de ${ua.sign}\n\tNascido em ${ua.weekDay}, $birthDateStr\n';
}
