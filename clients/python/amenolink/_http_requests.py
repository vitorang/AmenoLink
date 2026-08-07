import json
import urllib.request
import urllib.error
from ._shared import AmenoException


def _post_json(url: str, data: dict) -> dict:
    json_bytes = json.dumps(data).encode('utf-8')
    request = urllib.request.Request(
        url=url,
        data=json_bytes,
        headers={'Content-Type': 'application/json'},
        method='POST',
    )
    try:
        with urllib.request.urlopen(request) as response:
            if response.status != 200:
                raise AmenoException(f'Status HTTP inesperado: {response.status}')
            body = response.read().decode('utf-8')
            return json.loads(body) if body else {}
    except urllib.error.HTTPError as exception:
        raise AmenoException(f'Erro HTTP {exception.code}: {exception.reason}')
    except urllib.error.URLError as exception:
        raise AmenoException(f'Erro de conexão com o AmenoLink: {exception.reason}')
    except Exception as exception:
        if isinstance(exception, AmenoException):
            raise exception
        raise AmenoException(f'Falha na requisição: {str(exception)}')
