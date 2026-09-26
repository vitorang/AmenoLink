'''
    Exemplos de execução de Cache. Será demonstrado como manipular valores e grupos.

    CONFIGURAÇÃO
    - Execute o programa AmenoLink, vá na aba CACHES e adicione "example.cache" (sem aspas).
'''

from datetime import date
from time import sleep
from amenolink import cache, connection, ensure_ready, setup, Cache
from dtos import User


CACHE_GROUP = 'example.cache'


def basic_example(example_cache: Cache):
    gary_stu = User(name='Gary Stu', birth_date=date(2001, 1, 20))
    mary_sue = User(name='Mary Sue', birth_date=date(1988, 8, 19))

    # Valor não definido retorna None
    user = example_cache.get('gary', User)
    print(f'get: {user}\n')

    # Caso não exista, será criado
    user = example_cache.get_or_create('gary', lambda: gary_stu)
    print(f'get_or_create: {user}')
    print(f'get: {user}\n')

    # Definir e excluir valores
    example_cache.set('mary', mary_sue)
    user = example_cache.get('mary', User)
    print(f'set: {user}\n')
    example_cache.delete('mary')
    user = example_cache.get('mary', User)
    print(f'delete: {user}\n')

    # Obter todos os registros:
    example_cache.set('mary', mary_sue)
    example_cache.set('port', 13545)
    example_cache.set('true', True)
    entries = example_cache.all()
    print(f'all: {entries}\n')

    # Remover todos os registros:
    example_cache.clear()
    entries = example_cache.all()
    print(f'clear: {entries}\n')


def watcher_example(example_cache: Cache):
    def value_changed(key, value):
        print(f'[{key}]: {value}')
    
    def user_changed(user: User):
        print(f'> User: {user}')

    joe = User(name='Average Joe', birth_date=date(2010, 7, 12))
    jane = User(name='Average Jane', birth_date=date(2010, 12, 7))
    
    connection.connect()

    # Esse é o observador de alterações
    watcher = example_cache.watch()
    
    # Pode monitorar todas as alterações
    watcher.all(value_changed)
    example_cache.set('total', 5)
    example_cache.set('checked', False)

    # Ou monitorar uma chave específica
    watcher.key('user', user_changed)
    example_cache.set('user', joe)
    example_cache.set('user', jane)

    # Ao excluir valores, eles virão nulos
    example_cache.delete('user')
    example_cache.clear()
    sleep(1)

    # No fim, descarte o watcher para encerrar as inscrições
    watcher.dispose()
    example_cache.set('total', 9)
    sleep(1)
    example_cache.clear()
    connection.disconnect()
    connection.unsubscribe_all()


def main():
    setup(app_name='Cache Example (Python)')

    # Declaração do grupo de cache
    example_cache = cache(CACHE_GROUP)

    # Valida se os recursos estão configurados
    ensure_ready()

    basic_example(example_cache)
    watcher_example(example_cache)


if __name__ == '__main__':
    main()
