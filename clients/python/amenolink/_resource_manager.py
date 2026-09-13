from ._shared import AmenoException, client_setup
from ._http_requests import _post_json
from .dtos import Resources


class ResourceManager:
    def __init__(self):
        self.actions: set[str] = set()
        self.caches: set[str] = set()
        self.topics: set[str] = set()

    def ensure_ready(self) -> None:
        from importlib.metadata import version
        client_version = version('amenolink')

        resources = Resources(
            actions=list(self.actions),
            caches=list(self.caches),
            topics=list(self.topics),
            version=client_version,
        )

        url = f'{client_setup.origin_url}/api/resources/missing'
        response_data = _post_json(url, resources.to_dict())
        missing_resources = Resources.from_dict(response_data)

        if missing_resources.version != client_version:
            raise AmenoException(
                f'Versão incompatível do AmenoLink. Host: {missing_resources.version}, Cliente: {client_version}.'
            )

        missing_items: list[str] = []

        if missing_resources.actions:
            missing_items.append(f'Actions: {", ".join(missing_resources.actions)}')

        if missing_resources.caches:
            missing_items.append(f'Caches: {", ".join(missing_resources.caches)}')

        if missing_resources.topics:
            missing_items.append(f'Topics: {", ".join(missing_resources.topics)}')

        if missing_items:
            details = '\n'.join(missing_items)
            raise AmenoException(f'Recursos ausentes no AmenoLink:\n{details}')


resource_manager = ResourceManager()


def ensure_ready() -> None:
    resource_manager.ensure_ready()
