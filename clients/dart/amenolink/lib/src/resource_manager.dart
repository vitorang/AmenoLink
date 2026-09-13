import 'dtos.dart';
import 'http_requests.dart';
import 'shared.dart';

class ResourceManager {
  final Set<String> actions = {};
  final Set<String> caches = {};
  final Set<String> topics = {};

  Future<void> ensureReady() async {
    final resources = Resources(actions: actions.toList(), caches: caches.toList(), topics: topics.toList());

    final url = '${clientSetup.originUrl}/api/resources/missing';
    final responseData = await postJson(url, resources.toJson());
    final missingResources = Resources.fromJson(responseData);

    final missingItems = <String>[];

    if (missingResources.actions.isNotEmpty) {
      missingItems.add('Actions: ${missingResources.actions.join(', ')}');
    }

    if (missingResources.caches.isNotEmpty) {
      missingItems.add('Caches: ${missingResources.caches.join(', ')}');
    }

    if (missingResources.topics.isNotEmpty) {
      missingItems.add('Topics: ${missingResources.topics.join(', ')}');
    }

    if (missingItems.isNotEmpty) {
      throw AmenoException('Recursos ausentes no AmenoLink:\n${missingItems.join('\n')}');
    }
  }
}

final resourceManager = ResourceManager();

Future<void> ensureReady() => resourceManager.ensureReady();
