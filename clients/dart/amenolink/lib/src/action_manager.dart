import 'dart:convert';
import 'dart:io';
import 'package:amenolink/src/shared.dart' show clientSetup, parseData;

import 'dtos.dart';

const onStartupSuccess = '[AmenoLink.StartupSuccess]';
const onActionSuccess = '[AmenoLink.ActionSuccess]';
const onActionError = '[AmenoLink.ActionError]';
const onActionLogged = '[AmenoLink.ActionLog]';

typedef ActionHandler<T, R> = R Function(T input);

class ActionRoute {
  final String route;
  final Function handler;
  final dynamic Function(dynamic json) parseInput;

  ActionRoute({required this.route, required this.handler, required this.parseInput});
}

class ActionContext {
  final ActionRequest request;

  ActionContext(this.request);

  void log(String message) {
    sendMessage(onActionLogged, message);
  }
}

class ActionRouter {
  final List<ActionRoute> _routes = [];

  void add<T, R>({required String route, required ActionHandler<T, R> handler, T Function(dynamic json)? parseInput}) {
    _routes.add(
      ActionRoute(route: route, handler: handler, parseInput: parseInput ?? (json) => parseData<T>(json) as T),
    );
  }

  String _execute(ActionRequest request) {
    for (final route in _routes) {
      if (route.route == request.route) {
        final argument = route.parseInput(request.payload);
        final rawResult = Function.apply(route.handler, [argument]);
        return _formatResult(rawResult);
      }
    }
    throw ArgumentError("Rota '${request.route}' não encontrada");
  }

  String _formatResult(dynamic rawResult) {
    if (rawResult == null) return jsonEncode(null);
    if (rawResult is Map || rawResult is List || rawResult is String || rawResult is num || rawResult is bool) {
      return jsonEncode(rawResult);
    }
    try {
      return jsonEncode((rawResult as dynamic).toJson());
    } catch (_) {
      return jsonEncode(rawResult.toString());
    }
  }

  void serve() {
    sendMessage(onStartupSuccess, clientSetup.appName);

    final lines = stdin.transform(utf8.decoder).transform(const LineSplitter());

    lines.listen(
      (rawInput) {
        if (rawInput.trim().isEmpty) return;

        try {
          final decodedBytes = base64.decode(rawInput.trim());
          final rawJson = utf8.decode(decodedBytes);
          final requestMap = jsonDecode(rawJson) as Map<String, dynamic>;
          final request = ActionRequest.fromJson(requestMap);

          currentAction = ActionContext(request);
          final result = _execute(request);
          sendMessage(onActionSuccess, result);
        } catch (exception) {
          sendMessage(onActionError, exception.toString());
        } finally {
          currentAction = null;
        }
      },
      onError: (error) {
        sendMessage(onActionError, error.toString());
      },
    );
  }
}

final actions = ActionRouter();
ActionContext? currentAction;

ActionContext action() {
  final current = currentAction;
  if (current == null) {
    throw StateError('Nenhuma ação está em execução no momento.');
  }
  return current;
}

void sendMessage(String prefix, String message) {
  final payload = base64.encode(utf8.encode(message));
  stdout.writeln('$prefix$payload');
}
