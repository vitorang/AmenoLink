class User {
  final String name;
  final DateTime birthDate;

  User({required this.name, required this.birthDate});

  Map<String, dynamic> toJson() {
    final day = birthDate.day.toString().padLeft(2, '0');
    final month = birthDate.month.toString().padLeft(2, '0');
    final year = birthDate.year;

    return {'name': name, 'birthDate': '$day/$month/$year'};
  }

  factory User.fromJson(Map<String, dynamic> json) {
    final birthDateVal = json['birthDate'];
    DateTime parsedDate;

    if (birthDateVal is String && birthDateVal.isNotEmpty) {
      final parts = birthDateVal.split('/');
      if (parts.length == 3) {
        final day = int.parse(parts[0]);
        final month = int.parse(parts[1]);
        final year = int.parse(parts[2]);
        parsedDate = DateTime(year, month, day);
      } else {
        parsedDate = DateTime.parse(birthDateVal);
      }
    } else {
      parsedDate = DateTime.now();
    }

    return User(name: json['name'] as String? ?? '', birthDate: parsedDate);
  }

  @override
  String toString() {
    return 'User(name: $name, birthDate: ${toJson()['birthDate']})';
  }
}

class UserAstrology extends User {
  final String weekDay;
  final String sign;

  UserAstrology({required super.name, required super.birthDate, this.weekDay = '', this.sign = ''});

  @override
  Map<String, dynamic> toJson() {
    final data = super.toJson();
    data['weekDay'] = weekDay;
    data['sign'] = sign;
    return data;
  }

  factory UserAstrology.fromJson(Map<String, dynamic> json) {
    final user = User.fromJson(json);
    return UserAstrology(
      name: user.name,
      birthDate: user.birthDate,
      weekDay: json['weekDay'] as String? ?? '',
      sign: json['sign'] as String? ?? '',
    );
  }

  @override
  String toString() {
    return 'UserAstrology(name: $name, birthDate: ${toJson()['birthDate']}, weekDay: $weekDay, sign: $sign)';
  }
}

class Talk {
  final String author;
  final String text;
  final bool reply;

  Talk({required this.author, required this.text, this.reply = false});

  Map<String, dynamic> toJson() {
    return {'author': author, 'text': text, 'reply': reply};
  }

  factory Talk.fromJson(Map<String, dynamic> json) {
    return Talk(
      author: json['author'] as String? ?? '',
      text: json['text'] as String? ?? '',
      reply: json['reply'] as bool? ?? false,
    );
  }

  @override
  String toString() {
    return 'Talk(author: $author, text: $text, reply: $reply)';
  }
}
