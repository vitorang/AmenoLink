# AmenoLink

Durante meus estudos sobre arquiteturas *cloud*, me interessei bastante pelos conceitos de Serverless, FaaS (*Function as a Service*) e mensageria. No entanto, percebi que para reproduzir esses padrões em ambiente de desenvolvimento é frequentemente necessário lidar com contêineres e configurações complexas — algo que exige bastante pesquisa ou o uso de IA. Além disso, muitos serviços dependem de validação de licenças online, inviabilizando o funcionamento em redes privadas ou isoladas.

Com essas necessidades em mente, criei o **AmenoLink**: um projeto inspirado em serviços *cloud* que opera de forma nativa na própria máquina, totalmente offline, com configuração simplificada e sem a necessidade de contêineres ou dependências externas.

O programa foi desenvolvido em .NET + Angular (compatível com Windows). Adicionalmente, criei bibliotecas em diferentes linguagens para facilitar a integração com o AmenoLink.



## Recursos
Todos os recursos do AmenoLink são configuráveis por interface gráfica de forma simplificada, priorizando a Experiência do Desenvolvedor (DX - *Developer Experience*). Os exemplos completos das funcionalidades estão no diretório `/examples` em diferentes linguagens.

### Action
É a forma de atender requisições e filas, inspirado no padrão *Lambdalith* (ou *Monolithic Lambda*). Ao registrar um programa executável ou script Python, define todas as rotas que o programa atenderá. Quando uma requisição para a rota for feita, o AmenoLink iniciará o programa automaticamente, reaproveitará a mesma instância para requisições seguintes evitando a penalidade do *cold-start* e encerrará o processo quando ele ficar em desuso.


O número de instâncias do programa, tempos limites de inicialização e inatividade são configuráveis. O tempo limite de execução para cada *Action* é configurável individualmente por rotas.

Exemplo de declaração de *Action*:
```python
from amenolink import actions

def hello(name: str) -> str:
    return f'Hello, {name}!'

def bye(name: str) -> str:
    return f'See you later, {name}!'

actions.add('example.hello', hello)
actions.add('example.bye', bye)
actions.serve()
```

Há duas formas de executar ações, por requisição ou por fila. Toda *Action* publicará o resultado automaticamente no tópico de mesmo nome, abordarei na seção *Topic* como funciona.

```python
from amenolink import request, queue

greeting = request('example.hello', 'Gary Stu', str)
print(greeting)

queue('example.bye', 'Gary Stu')
```

Os dados enviados podem ser tipos primitivos ou instâncias de classes, mas não é possível enviar listas ou outras coleções diretamente. Essa decisão foi tomada para garantir portabilidade entre linguagens. Note que no Python é necessário indicar o tipo de dado!


### Cache
O *cache* é em memória, mas se diferencia dos programas por valores serem inseridos em grupos que possuem configurações pré-definidas. As configurações são tempo de expiração por desuso e tempo de vida total.


Exemplo de uso de *Cache*:
```python

from amenolink import cache

gary = User('Gary Stu', id='1')
mary = User('Mary Sue', id='2')

users = cache('example.users')
users.set(gary.id, gary)
users.set(mary.id, mary)

user = users.get('1', User)
```

### Topic (Pub/Sub)
Ao contrário do uso de filas, nenhum programa será iniciado automaticamente. A mensagem será enviada para todos os inscritos num tópico.

Use o método `connect` para iniciar a comunicação em tempo real com AmenoLink. Use `dispose` para destruir todas as inscrições daquela instância de `Topic`.

Toda mensagem é do tipo `TopicMessage<T>`, que conterá outras informações como todas as mensagens anteriores que originaram essa. A quantidade de chamadas anteriores é limitada por configuração para evitar problemas de loops infinitos acidentais. Quando uma atingir o limite, não haverá envio para os tópicos.

Exemplo de envio e recebimento de mensagem:
```python
from amenolink import connect, topic
from amenolink.dtos import TopicMessage
from time import sleep

def on_message_received(message: TopicMessage[str]):
    text = message.payload
    print(text)

chat = topic('example.logged', str)
chat.subscribe(on_message_received)

connect()

chat.publish("Who's there?")
sleep(1)
chat.dispose()
```

As mensagens vindas de `Action` serão `TopicMessage<ActionResponse<T>>`, porém é necessário que os nomes de ambos sejam os mesmos!

```python
from amenolink import connect, queue, topic
from amenolink.dtos import ActionResponse, TopicMessage
from time import sleep

def on_bye(message: TopicMessage[ActionResponse[str]]):
    response = message.payload
    print(response.result)

user = User('Mary Sue', id='2')

bye_topic = topic('example.bye', ActionResponse[str])
bye_topic.subscribe(on_bye)

connect()

queue('example.bye', user)
sleep(1)
bye_topic.dispose()
```

Note: você pode chamar `connect` após chamar o `subscribe`. A inscrição com tópicos é feita também quando ocorre a reconexão com AmenoLink. Mas uma vez feito `dispose`, a reconexão daquela instância não será refeita.

## Instruções de instalação
Em breve!

### Execução de exemplos
Em breve!

## Roadmap & Status do Projeto

### AmenoLink
- [x] Interface gráfica
- [x] Actions: execução de processos sob demanda
- [x] Cache em memória + eventos
- [x] Topics: Pub/Sub

### Bibliotecas de Clientes
- [x] Python
- [x] Dart
- [ ] TypeScript
- [ ] C#


## Decisões arquiteturais

### Isolamento fraco, simplicidade e leveza
Ao contrário de soluções *serverless*, AmenoLink não usa contêiner e faz uma gestão simplificada de processos. Focado em uso local, sem necessidade de escalabilidade horizontal, ele possui o consumo inicial de aproximadamente 20MB de RAM. O consumo pode variar de acordo com os dados em memória.

O nível de isolamento dos recursos é apenas por nome. Por exemplo, caso saiba o nome de um `Topic`, ou saiba o nome do grupo de um `Cache`, poderá acessar dados de outros programas que os usam.

### Interface Gráfica
A interface gráfica é um *webview* que abre um SPA desenvolvido em Angular Material. O programa pode ser minimizado para o *tray*, e quando é feito isso, o *webview* é destruído para economizar memória. Em compensação, quando o programa é reaberto, recarregará o SPA.

Para facilitar a inspeção de dados, a interface gráfica exibe as mensagens salvas por tópico e os dados salvos em cada grupo de *cache*, assim como os programas inscritos nos tópicos.

### Comunicação entre programas
A comunicação entre processos (*Actions*) é feita por mensagens em `base64` com prefixos indicadores através de `stdin/stdout` por ser de maior simplicidade que usar *sockets*. Por usar indicadores de inicialização e execução, o programa que fornecerá as *Actions* deverá usar a biblioteca para comunicação.

Entre programas que usam a API do AmenoLink, a comunicação é feita trafegando JSON por HTTP ou por SignalR. No Python há necessidade de informar os tipos por causa da conversão de JSON em instância de classe que a biblioteca fará automaticamente.

### Concorrência
A gestão de requisições e filas usa internamente `SemaphoreSlim`, se aproveitando do mecanismo que o .NET fornece. Como as travas funcionam por instâncias de programas, e não por rotas de *Actions*, não é recomendado usar o método `request()` para programas com *Actions* que fazem processamento pesado. É permitido cadastrar o mesmo programa várias vezes para driblar essa limitação, porém as rotas não podem se repetir.
