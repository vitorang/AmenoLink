'''
    Exemplos de execução de Action. Será demonstrado como usar recursos de requisição e filas.

    CONFIGURAÇÃO
    - No diretório onde está este arquivo, execute o comando para instalar as dependências Python:
        ./venv/Scripts/python.exe -m pip install -r requirements.txt

    - Execute o programa AmenoLink e vá na aba PROGRAMAS. Adicione action_server.py
        e adicione "example.action" (sem aspas) na seção Ações.

    - Na aba TÓPICOS, adicione "example.action".
'''

from amenolink import connect
from amenolink.dtos import ActionResponse
from datetime import date
from amenolink import request, queue, topic, ConnectionStatus
from amenolink.dtos import TopicMessage
from dtos import User, UserAstrology
from time import sleep

ACTION_ROUTE = 'example.action'


def main():
    gary_stu = User(name='Gary Stu', birth_date=date(2001, 1, 20))
    mary_sue = User(name='Mary Sue', birth_date=date(1988, 8, 19))
  
    # Abra uma conexão persistente
    connect(on_status_change=on_status_change)

    # Os resultados são publicados no tópico com mesmo nome da ação
    t = topic(ACTION_ROUTE, ActionResponse[UserAstrology])
    t.subscribe(on_message_received)

    # Com request poderá obter resultados de forma síncrona
    # e não precisará usar tópico ou conexão persistente.
    # Porém, o resultado será publicado no tópico!
    ua = request(ACTION_ROUTE, gary_stu, UserAstrology)
    print(f'Resposta de requisição: {format_user_astrology(ua)}')
    sleep(0.5)

    # Ou executar de forma assíncrona se não precisar do resultado ou o processamento for lento
    queue(ACTION_ROUTE, mary_sue)
    sleep(1)

    # Desativa conexão do tópico. Após isso, ele não poderá ser usado
    t.dispose()


def on_status_change(status: ConnectionStatus):
    print(f'Estado da conexão: {status}')


# A mensagem retornada de Action pela fila terá mensagem de resposta dentro da mensagem de tópico
def on_message_received(message: TopicMessage[ActionResponse[UserAstrology]]):
    sleep(0.1)
    response = message.payload
    print(f'Mensagem do tópico: \n\tLogs: {response.logs}{format_user_astrology(response.result)}')


def format_user_astrology(ua: UserAstrology):
    birth_date = ua.birth_date.strftime('%d/%m/%Y')
    return f'\n\t{ua.name} de {ua.sign}\n\tNascido em {ua.week_day}, {birth_date}\n'


if __name__ == '__main__':
    main()