import json
import urllib.request
import urllib.error


def _post_json(url: str, data: dict) -> dict:
    json_bytes = json.dumps(data).encode('utf-8')
    request = urllib.request.Request(
        url=url,
        data=json_bytes,
        headers={'Content-Type': 'application/json'},
        method='POST',
    )
    with urllib.request.urlopen(request) as response:
        body = response.read().decode('utf-8')
        return json.loads(body) if body else {}
