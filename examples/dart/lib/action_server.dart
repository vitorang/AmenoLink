/*
  Servidor de Action que será executado pelo AmenoLink.
  Gere o exe usando comando:
    dart compile exe action_server.dart -o action_server.exe
  
  NÃO execute-o manualmente. Ele é usado pelo action_example.dart.
*/

import 'package:amenolink/amenolink.dart';
import 'dtos.dart';

void main() {
  // Configuração inicial do programa. É recomendado definir app_name,
  // porém, se o AmenoLink está rodando localmente, não é necessário definir origin_url.
  setup(
    appName: 'Action Server (Dart)',
    originUrl: 'http://localhost:13545', // Valor padrão
  );

  // Registre os tipos usados
  registerType<User>(User.fromJson);
  registerType<UserAstrology>(UserAstrology.fromJson);

  // Para registrar uma ação, a rota é igual à registrada no AmenoLink.
  actions.add<User, UserAstrology>(route: 'example.action', handler: hello);

  // Aguarda por requisições.
  actions.serve();
}

String getWeekDay(DateTime dt) {
  final days = ['Segunda-feira', 'Terça-feira', 'Quarta-feira', 'Quinta-feira', 'Sexta-feira', 'Sábado', 'Domingo'];
  return days[dt.weekday - 1];
}

String getZodiacSign(DateTime dt) {
  final day = dt.day;
  final month = dt.month;

  if ((month == 3 && day >= 21) || (month == 4 && day <= 19)) return 'Áries';
  if ((month == 4 && day >= 20) || (month == 5 && day <= 20)) return 'Touro';
  if ((month == 5 && day >= 21) || (month == 6 && day <= 20)) return 'Gêmeos';
  if ((month == 6 && day >= 21) || (month == 7 && day <= 22)) return 'Câncer';
  if ((month == 7 && day >= 23) || (month == 8 && day <= 22)) return 'Leão';
  if ((month == 8 && day >= 23) || (month == 9 && day <= 22)) return 'Virgem';
  if ((month == 9 && day >= 23) || (month == 10 && day <= 22)) return 'Libra';
  if ((month == 10 && day >= 23) || (month == 11 && day <= 21)) return 'Escorpião';
  if ((month == 11 && day >= 22) || (month == 12 && day <= 21)) return 'Sagitário';
  if ((month == 12 && day >= 22) || (month == 1 && day <= 19)) return 'Capricórnio';
  if ((month == 1 && day >= 20) || (month == 2 && day <= 18)) return 'Aquário';

  return 'Peixes';
}

// Assim é declarada uma ação.
UserAstrology hello(User user) {
  final weekDay = getWeekDay(user.birthDate);
  final sign = getZodiacSign(user.birthDate);

  // Use action() para obter contexto da ação atual.
  action().log('Olá, ${user.name}!');
  action().log('Você é de $sign e nasceu $weekDay!');

  return UserAstrology(name: user.name, birthDate: user.birthDate, weekDay: weekDay, sign: sign);
}
