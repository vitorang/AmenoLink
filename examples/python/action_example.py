'''
    Exemplos de execução de Action. Será demonstrado como usar recursos de requisição e filas.

    CONFIGURAÇÃO
    - No diretório onde está este arquivo, execute o comando para instalar as dependências Python:
        ./venv/Scripts/python.exe -m pip install -r requirements.txt

    - Execute o programa AmenoLink e vá na aba PROGRAMAS. Adicione action_server.py
        e adicione "example.action" (sem aspas) na seção Ações.

    - Na aba TÓPICOS, adicione "example.action".
'''

from amenolink import action, connection, ensure_ready, setup, topic, ConnectionStatus
from amenolink.dtos import ActionResponse, TopicMessage
from datetime import date
from dtos import User, UserAstrology
from time import sleep

ACTION_ROUTE = 'example.action'


def main():
    setup(app_name='Action Example (Python)')

    # Declaração dos recursos utilizados
    example_action = action(ACTION_ROUTE)
    action_topic = topic(ACTION_ROUTE, ActionResponse[UserAstrology])

    # Valida se os recursos estão configurados
    ensure_ready()

    gary_stu = User(name='Gary Stu', birth_date=date(2001, 1, 20))
    mary_sue = User(name='Mary Sue', birth_date=date(1988, 8, 19))

    # Inscreve para receber eventos de status da conexão
    connection.subscribe(on_status_change)

    # Abre uma conexão persistente
    connection.connect()


    # Os resultados são publicados no tópico com mesmo nome da ação
    action_topic.subscribe(on_message_received)

    # Com request poderá obter resultados de forma síncrona
    # e não precisará usar tópico ou conexão persistente.
    # Porém, o resultado será publicado no tópico!
    astrology = example_action.request(gary_stu, UserAstrology)
    print(f'Resposta de requisição: {format_user_astrology(astrology)}')
    sleep(0.5)

    # Ou executar de forma assíncrona se não precisar do resultado ou o processamento for lento
    example_action.queue(mary_sue)
    sleep(1)

    # Desativa conexão do tópico. Após isso, ele não poderá ser usado
    action_topic.dispose()

    # Fecha a conexão
    connection.disconnect()

    # Pode desinscrever um evento
    connection.unsubscribe(on_status_change)

    # Ou desinscrever todos globalmente
    connection.unsubscribe_all()



def on_status_change(status: ConnectionStatus):
    print(f'Estado da conexão: {status}')


# Actions adicionam ActionResponse no payload de TopicMessage
def on_message_received(message: TopicMessage[ActionResponse[UserAstrology]]):
    sleep(0.1)
    response = message.payload
    print(f'Mensagem do tópico: \n\tLogs: {response.logs}{format_user_astrology(response.result)}')


def format_user_astrology(ua: UserAstrology):
    birth_date = ua.birth_date.strftime('%d/%m/%Y')
    return f'\n\t{ua.name} de {ua.sign}\n\tNascido em {ua.week_day}, {birth_date}\n'


if __name__ == '__main__':
    main()