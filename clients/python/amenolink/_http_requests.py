import json
import urllib.request
import urllib.error
from ._shared import AmenoException, T, _parse_data


class OriginUrl:
    def __init__(self):
        self._url: str = 'http://localhost:13545'

    def get(self) -> str:
        return self._url

    def set(self, url: str) -> None:
        self._url = url.rstrip('/')


origin_url = OriginUrl()


def _post_json(url: str, data: dict) -> dict:
    json_bytes = json.dumps(data).encode('utf-8')
    req = urllib.request.Request(
        url=url,
        data=json_bytes,
        headers={'Content-Type': 'application/json'},
        method='POST',
    )
    try:
        with urllib.request.urlopen(req) as resp:
            if resp.status != 200:
                raise AmenoException(f'Status HTTP inesperado: {resp.status}')
            body = resp.read().decode('utf-8')
            return json.loads(body) if body else {}
    except urllib.error.HTTPError as e:
        raise AmenoException(f'Erro HTTP {e.code}: {e.reason}')
    except urllib.error.URLError as e:
        raise AmenoException(f'Erro de conexão com o AmenoLink: {e.reason}')
    except Exception as e:
        if isinstance(e, AmenoException):
            raise e
        raise AmenoException(f'Falha na requisição: {str(e)}')
