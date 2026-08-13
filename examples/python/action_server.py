'''
    Servidor de Action que será executado pelo AmenoLink.
    NÃO execute-o manualmente. Ele é usado pelo action_example.py.
'''

from datetime import date
from dtos import UserAstrology, User
from amenolink import actions, action, setup


# Configuração inicial do programa. É recomendado definir app_name,
# porém, se o AmenoLink está rodando localmente, não é necessário definir origin_url.
setup(
    app_name='Action Server (Python)',
    origin_url='http://localhost:13545' # Valor padrão
)


def get_week_day(dt: date) -> str:
    days = [
        'Segunda-feira',
        'Terça-feira',
        'Quarta-feira',
        'Quinta-feira',
        'Sexta-feira',
        'Sábado',
        'Domingo'
    ]
    return days[dt.weekday()]


def get_zodiac_sign(dt: date) -> str:
    day = dt.day
    month = dt.month

    if (month == 3 and day >= 21) or (month == 4 and day <= 19):
        return 'Áries'
    elif (month == 4 and day >= 20) or (month == 5 and day <= 20):
        return 'Touro'
    elif (month == 5 and day >= 21) or (month == 6 and day <= 20):
        return 'Gêmeos'
    elif (month == 6 and day >= 21) or (month == 7 and day <= 22):
        return 'Câncer'
    elif (month == 7 and day >= 23) or (month == 8 and day <= 22):
        return 'Leão'
    elif (month == 8 and day >= 23) or (month == 9 and day <= 22):
        return 'Virgem'
    elif (month == 9 and day >= 23) or (month == 10 and day <= 22):
        return 'Libra'
    elif (month == 10 and day >= 23) or (month == 11 and day <= 21):
        return 'Escorpião'
    elif (month == 11 and day >= 22) or (month == 12 and day <= 21):
        return 'Sagitário'
    elif (month == 12 and day >= 22) or (month == 1 and day <= 19):
        return 'Capricórnio'
    elif (month == 1 and day >= 20) or (month == 2 and day <= 18):
        return 'Aquário'
    else:
        return 'Peixes'


# Assim é declarado uma ação.
def hello(user: User) -> UserAstrology:
    week_day = get_week_day(user.birth_date)
    sign = get_zodiac_sign(user.birth_date)

    # Use action() para obter contexto da ação atual.
    action().log(f'Olá, {user.name}!')
    action().log(f'Você é de {sign} e nasceu {week_day}!')

    return UserAstrology(
        name=user.name,
        birth_date=user.birth_date,
        week_day=week_day,
        sign=sign
    )

# Para registrar uma ação, a rota é igual à registrada no AmenoLink.
actions.add('example.action', hello)


# Aguarda por requisições.
if __name__ == '__main__':
    actions.serve()
