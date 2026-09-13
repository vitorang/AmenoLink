'''
    Exemplos de tópicos. Será demonstrado como publicar e receber mensagens.

    CONFIGURAÇÃO
    - Execute o programa AmenoLink, vá na aba TÓPICOS e adicione "example.topic" (sem aspas).
'''


from amenolink import connect, disconnect, ensure_ready, setup, topic, Topic
from amenolink.dtos import TopicMessage
from dtos import Talk
from time import sleep


TOPIC_NAME = 'example.topic'


def main():
    setup(app_name='Topic Example (Python)')

    # Declaração dos tópicos
    listener = topic(TOPIC_NAME, Talk)
    sender = topic(TOPIC_NAME, Talk)

    # Valida se os recursos estão configurados
    ensure_ready()

    # A conexão é compartilhada por projeto,
    # recursos declarados antes ou após a usarão.
    connect()

    def reply(message: TopicMessage[Talk]):
        sleep(0.5)
        talk = message.payload
        if not talk.reply:
            return

        reply_talk = Talk(author='Bot', text='Não há ninguém por aqui. Você será desconectado.', reply=False)
        # Ao enviar uma mensagem que é resposta a outra, envie a anterior para manter histórico
        # Caso haja loop de chamadas consecutivas, isso evitará loop infinito
        sender.publish(reply_talk, message)

    listener.subscribe(show_talk)
    listener.subscribe(reply)

    talk = Talk(author='Gary Stu', text='Olá!', reply=False)
    sender.publish(talk)
    sleep(1)

    talk = Talk(author='Gary Stu', text='Alguém por aí?', reply=True)
    sender.publish(talk)
    sleep(1)

    # Ao usar dispose, elimina todas as inscrições daquela instância
    listener.dispose()

    talk = Talk(author='Gary Stu', text='Por quê???', reply=False)
    # Não há inscritos para receber
    sender.publish(talk)
    sleep(0.5)
    sender.dispose()
    disconnect()


def show_talk(message: TopicMessage[Talk]):
    talk = message.payload
    print(f'{talk.author}: {talk.text}')


if __name__ == '__main__':
    main()