/*
    Exemplos de tópicos. Será demonstrado como publicar e receber mensagens.

    CONFIGURAÇÃO
    - Execute o programa AmenoLink, vá na aba TÓPICOS e adicione "example.topic" (sem aspas).
*/

import 'dart:io';
import 'package:amenolink/amenolink.dart';
import 'dtos.dart';

const topicName = 'example.topic';

void main() async {
  registerType<Talk>(Talk.fromJson);
  setup(appName: 'Topic Example (Dart)');

  // Duas interfaces para conectar ao mesmo tópico
  final listener = topic<Talk>(topicName);
  final sender = topic<Talk>(topicName);

  // Valida se os recursos estão configurados
  await ensureReady();

  // A conexão é compartilhada por projeto,
  // recursos declarados antes ou após a usarão.
  await connection.connect();

  void reply(TopicMessage<Talk> message) async {
    await Future.delayed(const Duration(milliseconds: 500));
    final talk = message.payload;
    if (talk == null || !talk.reply) {
      return;
    }

    final replyTalk = Talk(author: 'Bot', text: 'Não há ninguém por aqui. Você será desconectado.', reply: false);
    // Ao enviar uma mensagem que é resposta a outra, envie a anterior para manter histórico
    // Caso haja loop de chamadas consecutivas, isso evitará loop infinito
    await sender.publish(replyTalk, previous: message);
  }

  listener.subscribe(showTalk);
  listener.subscribe(reply);

  var talk = Talk(author: 'Gary Stu', text: 'Olá!', reply: false);
  await sender.publish(talk);
  await Future.delayed(const Duration(seconds: 1));

  talk = Talk(author: 'Gary Stu', text: 'Alguém por aí?', reply: true);
  await sender.publish(talk);
  await Future.delayed(const Duration(seconds: 1));

  // Ao usar dispose, elimina todas as inscrições daquela instância
  listener.dispose();

  talk = Talk(author: 'Gary Stu', text: 'Por quê???', reply: false);
  // Não há inscritos para receber
  await sender.publish(talk);
  await Future.delayed(const Duration(milliseconds: 500));
  sender.dispose();
  await connection.disconnect();
  connection.unsubscribeAll();

  exit(0);
}

void showTalk(TopicMessage<Talk> message) {
  final talk = message.payload;
  if (talk != null) {
    print('${talk.author}: ${talk.text}');
  }
}
