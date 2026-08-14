'''
    Exemplos de execução de Cache. Será demonstrado como manipular valores e grupos.

    CONFIGURAÇÃO
    - Execute o programa AmenoLink, vá na aba CACHES e adicione "example.cache" (sem aspas).
'''

from datetime import date
from time import sleep
from amenolink import cache, connect, disconnect, setup
from dtos import User


def basic_example():
    gary_stu = User(name='Gary Stu', birth_date=date(2001, 1, 20))
    mary_sue = User(name='Mary Sue', birth_date=date(1988, 8, 19))

    # Esse é o grupo de valores
    c = cache('example.cache')

    # Valor não definido retorna None
    user = c.get('gary', User)
    print(f'get: {user}\n')

    # Caso não exista, será criado
    user = c.get_or_create('gary', lambda: gary_stu)
    print(f'get_or_create: {user}')
    print(f'get: {user}\n')

    # Definir e excluir valores
    c.set('mary', mary_sue)
    user = c.get('mary', User)
    print(f'set: {user}\n')
    c.delete('mary')
    user = c.get('mary', User)
    print(f'delete: {user}\n')

    # Obter todos os registros:
    c.set('mary', mary_sue)
    c.set('port', 13545)
    c.set('true', True)
    map = c.all()
    print(f'all: {map}\n')

    # Remover todos os registros:
    c.clear()
    map = c.all()
    print(f'clear: {map}\n')


def watcher_example():
    def value_changed(key, value):
        print(f'[{key}]: {value}')
    
    def user_changed(user: User):
        print(f'> User: {user}')

    joe = User(name='Average Joe', birth_date=date(2010, 7, 12))
    jane = User(name='Average Jane', birth_date=date(2010, 12, 7))
    c = cache('example.cache')
    
    connect()

    # Esse é o observador de alterações
    w = c.watch()
    
    # Pode monitorar todas as alterações
    w.all(value_changed)
    c.set('total', 5)
    c.set('checked', False)

    # Ou monitorar uma chave específica
    w.key('user', user_changed)
    c.set('user', joe)
    c.set('user', jane)

    # Ao excluir valores, eles virão nulos
    c.delete('user')
    c.clear()
    sleep(1)

    # No fim, descarte o watcher para encerrar as inscrições
    w.dispose()
    c.set('total', 9)
    sleep(1)
    c.clear()
    disconnect()


if __name__ == '__main__':
    setup(app_name='Cache Example (Python)')
    basic_example()
    watcher_example()
