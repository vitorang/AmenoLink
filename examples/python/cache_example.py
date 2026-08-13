'''
    Exemplos de execução de Cache. Será demonstrado como manipular valores e grupos.

    CONFIGURAÇÃO
    - Execute o programa AmenoLink, vá na aba CACHES e adicione "example.cache" (sem aspas).
'''

from amenolink import cache, setup
from datetime import date
from dtos import User


def main():
    setup(app_name='Cache Example (Python)')

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


if __name__ == '__main__':
    main()

