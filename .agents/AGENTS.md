# Diretrizes do Projeto AmenoLink

## Regras de Código e Estilo
- **Backend (.NET / C#):** Usar Minimal APIs em `WebApi/`, records imutáveis e convenções de nomenclatura limpas. Em if de uma linha, deixe sem chaves e com instruções abaixo.
- **Frontend (Angular):** Usar componentes Standalone, Angular Signals (`signal`, `computed`, `input`), Angular Material com tema escuro e layout responsivo/compacto. Em if de uma linha, deixe sem chaves e com instruções abaixo.
- **Estrutura:** Frontend vive em `AmenoLink.WebUI/` na raiz do repositório.
- Evite colocar comentários no código.
- Evite abreviar nome de variáveis.
- Prefira aspas simples no Python e TypeScript.

## Regras do Domínio
- Nomes de action, cache, store, topic, assim como os groups do SignalR são case-sensitive.