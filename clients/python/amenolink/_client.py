class AmenoException(Exception):
    def __init__(self, message: str):
        super().__init__(message)
        self.message = message


class OriginUrl:
    def __init__(self):
        self._url: str = 'http://localhost:13545'

    def get(self) -> str:
        return self._url

    def set(self, url: str) -> None:
        self._url = url.rstrip('/')


origin_url = OriginUrl()
