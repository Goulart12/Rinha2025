# Rinha De Backend 2025

Este projeto é uma implementação para a Rinha de Backend de 2025, focando em uma arquitetura assíncrona para processamento de pagamentos.

## Arquitetura

A aplicação é composta por múltiplos serviços orquestrados via Docker Compose com  limites de memória (350MB) e CPU (1.5). A arquitetura inclui:

- **Serviços de Backend (.NET 9.0):** Duas instâncias da aplicação principal (`backend1` e `backend2`) desenvolvidas em .NET 9.0, responsáveis por receber e processar requisições de pagamento.
- **Load Balancer (Nginx):** Um servidor Nginx atua como balanceador de carga, distribuindo as requisições entre as instâncias do backend.
- **Redis:** Uma instância do Redis é utilizada para armazenamento de dados e sumarização de pagamentos.
- **Processadores de Pagamento Externos:** A aplicação interage com processadores de pagamento externos (um padrão e um de fallback), cujas URLs são configuráveis via variáveis de ambiente.

## Serviços

### `backend1` e `backend2`
- **Tecnologia:** .NET 9.0
- **Função:** Recebe requisições de pagamento, enfileira-as para processamento assíncrono e fornece sumarização de pagamentos. Interage com o Redis e com os processadores de pagamento externos.
- **Variáveis de Ambiente:**
    - `ASPNETCORE_URLS`: URL onde a aplicação .NET irá escutar (ex: `http://*:8080`).
    - `REDIS_CONNECTION_STRING`: String de conexão com o Redis (ex: `redis:6379`).
    - `PROCESSOR_DEFAULT_URL`: URL do processador de pagamento padrão.
    - `PROCESSOR_FALLBACK_URL`: URL do processador de pagamento de fallback.
- **Limites de Recursos (Docker Compose):**
    - CPU: 0.55
    - Memória: 110MB

### `nginx`
- **Tecnologia:** Nginx 1.25-alpine
- **Função:** Atua como um balanceador de carga, distribuindo as requisições HTTP para as instâncias do `backend`.
- **Limites de Recursos (Docker Compose):**
    - CPU: 0.15
    - Memória: 15MB

### `redis`
- **Tecnologia:** Redis 7.2-alpine
- **Função:** Armazenamento de dados e suporte para a sumarização de pagamentos.
- **Limites de Recursos (Docker Compose):**
    - CPU: 0.25
    - Memória: 75MB

## Endpoints da API

### `POST /payments`
- **Descrição:** Envia uma requisição de pagamento para ser processada. O pagamento é enfileirado e processado assincronamente.
- **Método:** `POST`
- **Corpo da Requisição (JSON):**
    ```json
    {
        "correlationId": "72910451-66ba-464b-a374-d04f61d9980c",
        "amount": 100.50,
        "requestedAt": "2025-01-01T00:00:00Z"
    }
    ```
- **Resposta:**
    - `202 Accepted`: Indica que o pagamento foi recebido e enfileirado para processamento.
    ```json
    {
        "message": "payment received and queued for processing"
    }
    ```

### `GET /payments-summary`
- **Descrição:** Retorna um resumo dos pagamentos processados dentro de um período especificado.
- **Método:** `GET`
- **Parâmetros de Query:**
    - `from` (DateTime): Data de início do período para o resumo (ex: `2025-01-01T00:00:00Z`).
    - `to` (DateTime): Data de fim do período para o resumo (ex: `2025-01-31T23:59:59Z`).
- **Resposta:**
    - `200 OK`: Retorna um objeto JSON com o resumo dos pagamentos, agrupados por processador.
    ```json
    {
        "default": {
            "totalRequests": 150,
            "totalAmount": 15000.75
        },
        "fallback": {
            "totalRequests": 25,
            "totalAmount": 2500.50
        }
    }
    ```

## Repositório da Rinha feito por [zanfranceschi](https://github.com/zanfranceschi).

[Rinha de backend](https://github.com/zanfranceschi/rinha-de-backend-2025)
