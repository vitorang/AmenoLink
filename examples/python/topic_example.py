'''
    Exemplos de tópicos. Será demonstrado como publicar e receber mensagens.

    CONFIGURAÇÃO
    - Execute o programa AmenoLink, vá na aba TÓPICOS e adicione "example.topic" (sem aspas).
'''


from amenolink.dtos import TopicMessage
from dtos import Talk
from amenolink import connect, setup, topic
from time import sleep

# Duas interfaces para conectar ao mesmo tópico
listener = topic('example.topic', Talk)
sender = topic('example.topic', Talk)

def main():
    setup(app_name='Topic Example (Python)')

    # Você pode se conectar após declarar os tópicos.
    connect()
   
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


def show_talk(message: TopicMessage[Talk]):
    talk = message.payload
    print(f'{talk.author}: {talk.text}')


def reply(message: TopicMessage[Talk]):
    sleep(0.5)
    talk = message.payload
    if not talk.reply:
        return

    talk = Talk(author='Bot', text='Não há ninguém por aqui. Você será desconectado.', reply=False)
    # Ao enviar uma mensagem que é resposta a outra, envie a anterior para manter histórico
    # Caso haja loop de chamadas consecutivas, isso evitará loop infinito
    sender.publish(talk, message)
    
if __name__ == '__main__':
    main()