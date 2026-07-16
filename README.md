# Order Routing Sample | FIX Protocol (Financial Information eXchange)

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-19.2-61DAFB?style=flat-square&logo=react)](https://react.dev/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-336791?style=flat-square&logo=postgresql)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=flat-square&logo=docker)](https://www.docker.com/)
[![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)](LICENSE)

## Descrição

Sistema de roteamento de pedidos para mercados de capitais utilizando protocolo FIX 4.4 com validação de exposição em tempo real. Implementado em arquitetura de Vertical Slices com padrão CQRS, demonstrando uma solução enterprise-grade para processamento de ordens de trading com garantias de consistência e rastreabilidade.

## Arquitetura

```
┌──────────────────────────────────────────────────────────────┐
│                    React Frontend                             │
│  (Browser - Port 5173)                                       │
├──────────────────────────────────────────────────────────────┤
│  - Order Form                                                 │
│  - Real-time Status Updates                                   │
│  - TypeScript + Vite                                          │
└──────────────────────────┬──────────────────────────────────┘
                           │ HTTP/REST
                           ↓
┌──────────────────────────────────────────────────────────────┐
│            Order Generator API (Port 5000)                    │
│  REST Endpoints + JWT Authentication                         │
├──────────────────────────────────────────────────────────────┤
│  - POST /orders (Create)                                      │
│  - GET /orders (List)                                         │
│  - GET /health (Health Check)                                 │
│                                                               │
│  Internals:                                                   │
│  - MediatR CQRS Pattern                                       │
│  - FluentValidation                                           │
│  - Repository Pattern                                         │
│  - OpenTelemetry Instrumentation                              │
│  - Serilog Structured Logging                                 │
└──────────────────────────┬──────────────────────────────────┘
                           │ FIX 4.4 Protocol
                           │ (TCP/Initiator)
                           ↓
┌──────────────────────────────────────────────────────────────┐
│         Order Accumulator Worker (Port 9000)                  │
│  FIX Protocol Server + Order Processing                      │
├──────────────────────────────────────────────────────────────┤
│  - FIX Acceptor (NewOrderSingle, ExecutionReport)            │
│  - Exposure Calculator                                        │
│  - Event Store Pattern                                        │
│  - Order Execution (Accept/Reject)                            │
│  - OpenTelemetry Instrumentation                              │
│  - Serilog Structured Logging                                 │
└──────────────┬──────────────────────────┬────────────────────┘
               │                          │
               ↓ (Read/Write)             ↓ (Cache)
        ┌─────────────┐          ┌─────────────────┐
        │ PostgreSQL  │          │     Redis       │
        │    (Port    │          │   (Port 6379)   │
        │   5432)     │          │                 │
        └─────────────┘          └─────────────────┘
```

## Requisitos

### Obrigatórios
- Docker 20.10+
- Docker Compose 2.0+

### Stack
**Backend:**
- .NET 8.0 (14 projetos com Vertical Slice Architecture)
- MediatR 11.1.0 (CQRS Pattern)
- Entity Framework Core 8.0
- PostgreSQL 15
- StackExchange.Redis 2.6
- QuickFIX/n 14.5.1 (FIX Protocol)
- OpenTelemetry 1.8.0 (Observability)
- Serilog 3.1.1 (Structured Logging)
- FluentValidation 11.9.2

**Frontend:**
- React 19.2
- TypeScript 6.0
- Vite 8.1
- Fetch API (HTTP Client)

**Infraestrutura:**
- Docker & Docker Compose
- PostgreSQL 15
- Redis 7
- Keycloak (autenticação)

## Como Rodar
![Order Routing Demo](demo-order-routing.gif)

### Pré-requisitos

```bash
docker --version
docker-compose --version
```

### Build e Execução

```bash
# 1. Clonar repositório
cd ~/fix-order-routing-sample

# 2. Build das imagens
cd .docker
docker-compose build

# 3. Iniciar todos os serviços
docker-compose up

# Aguarde mensagens de sucesso:
# fix-postgres is healthy
# fix-redis is healthy
# fix-generator-api | Now listening on: http://0.0.0.0:5000
# fix-accumulator-worker | FIX Acceptor session created
# fix-frontend | ✓ built in XXms
```

### URLs de Acesso

| Serviço | URL | Descrição |
|---------|-----|-----------|
| Frontend | http://localhost:5173 | React App |
| API | http://localhost:5000/api/v1 | REST Endpoints |
| Health | http://localhost:5000/api/v1/health | Status DB/Cache |
| Keycloak | http://localhost:8080 | Autenticação |

### Logs em Tempo Real (Terminal 2)

```bash
docker-compose -f .docker/docker-compose.yml logs -f fix-generator-api
```

## Documentação de Negócio

### Estados de Ordem

```
┌──────────┐
│Submitted │ Ordem recebida pela API
└────┬─────┘
     │ Enviada via FIX para Accumulator
     ↓
┌──────────┐
│ Pending  │ Validação de exposição em progresso
└────┬─────┘
     │
     ├─── Passou validação ──→ ┌──────────┐
     │                         │ Accepted │ ✓ Ordem aceita
     │                         └──────────┘
     │
     └─── Violou limite ──→ ┌──────────┐
                            │ Rejected │ ✗ Exposição excedida
                            └──────────┘
```

### Validações

**Exposição por Símbolo:**
- Limite: -100M a +100M por símbolo
- Cálculo: `(Quantidade * Preço) + Exposição Atual`
- Tipo: Long positivo (compra), Short negativo (venda)

**Validações de Pedido:**
- Símbolo: PETR4, VALE3, VIIA4
- Quantidade: >= 1
- Preço: > 0
- Lado: BUY ou SELL
- TimeInForce: GTC (Good-Till-Cancel)

### Fluxo Completo

1. Frontend: Usuário preenche formulário
2. API: Valida estrutura, autentica JWT, persiste Order (status: Submitted)
3. FIX Initiator: Envia NewOrderSingle para Accumulator
4. FIX Acceptor: Recebe ordem, calcula exposição
5. Decision: Compara exposição com limite
6. Execution: Cria OrderExecution (Accepted/Rejected)
7. FIX Initiator: Recebe ExecutionReport
8. API: Atualiza status e notifica Frontend
9. Frontend: Mostra status em tempo real

## Diagramas C4

### Contexto do Sistema

```mermaid
graph TB
    User["Usuário (Trader)"]
    Frontend["React Frontend"]
    API["Order Generator API"]
    Accumulator["Order Accumulator Worker"]
    DB["PostgreSQL"]
    Cache["Redis"]
    Auth["Keycloak"]

    User -->|Cria Pedido| Frontend
    Frontend -->|HTTP REST| API
    API -->|FIX 4.4 Protocol| Accumulator
    API -->|Read/Write| DB
    API -->|Token Cache| Cache
    API -->|Verifica JWT| Auth
    Accumulator -->|Event Store| DB
    Accumulator -->|Exposure Cache| Cache
    
    classDef external fill:#E1F5FE,stroke:#01579B,stroke-width:2px
    classDef system fill:#F3E5F5,stroke:#4A148C,stroke-width:2px
    classDef data fill:#E8F5E9,stroke:#1B5E20,stroke-width:2px
    
    class User,Auth external
    class Frontend,API,Accumulator system
    class DB,Cache data
```

### Sequência: Criar Pedido

```mermaid
sequenceDiagram
    actor User
    participant Frontend as React<br/>Frontend
    participant API as Order<br/>Generator
    participant Fix as FIX<br/>Accumulator
    participant DB as PostgreSQL
    participant Cache as Redis

    User->>Frontend: Preenche Form (PETR4, 100, 25.50)
    Frontend->>API: POST /orders + JWT
    
    rect rgb(200, 230, 255)
        Note over API: Validação
        API->>DB: Cria Order (Submitted)
        DB-->>API: Order persisted
    end
    
    rect rgb(255, 230, 200)
        Note over API,Fix: FIX Protocol
        API->>Fix: NewOrderSingle
        Fix->>Cache: GET exposição PETR4
        Cache-->>Fix: -50M
        Fix->>Fix: Calcula: 100 * 25.50 = 2550<br/>Nova exposição: -50M + 2550 = -47.45M<br/>Válido? SIM (dentro de [-100M, +100M])
        Fix->>DB: Cria OrderExecution (Accepted)
        Fix->>API: ExecutionReport (Accepted)
        API->>Cache: SET exposição PETR4 = -47.45M
    end
    
    rect rgb(230, 255, 200)
        Note over API: Update Status
        API->>DB: Update Order.Status = Accepted
        API-->>Frontend: JSON (status: Accepted)
        Frontend-->>User: Mostra ordem aceita
    end
```

### Sequência: Rejeição por Exposição

```mermaid
sequenceDiagram
    actor User
    participant Frontend as React<br/>Frontend
    participant API as Order<br/>Generator
    participant Fix as FIX<br/>Accumulator
    participant DB as PostgreSQL
    participant Cache as Redis

    User->>Frontend: Preenche Form (PETR4, 10M, 25.50)
    Frontend->>API: POST /orders + JWT
    
    rect rgb(200, 230, 255)
        Note over API: Validação
        API->>DB: Cria Order (Submitted)
    end
    
    rect rgb(255, 230, 200)
        Note over API,Fix: FIX Protocol
        API->>Fix: NewOrderSingle
        Fix->>Cache: GET exposição PETR4
        Cache-->>Fix: -95M
        Fix->>Fix: Calcula: 10M * 25.50 = 255M<br/>Nova exposição: -95M + 255M = +160M<br/>Válido? NÃO (excede +100M)
        Fix->>DB: Cria OrderExecution (Rejected)<br/>Reason: Exposure Limit Exceeded
        Fix->>API: ExecutionReport (Rejected)
    end
    
    rect rgb(255, 200, 200)
        Note over API: Update Status
        API->>DB: Update Order.Status = Rejected<br/>RejectionReason: Exposure Limit Exceeded
        API-->>Frontend: JSON (status: Rejected, reason: ...)
        Frontend-->>User: Mostra ordem rejeitada
    end
```

## Endpoints da API

### Autenticação

```bash
POST /api/v1/token/debug
# Retorna: { "token": "eyJ..." }
# Nota: Endpoint de debug, sem autenticação requerida
```

### Pedidos

```bash
POST /api/v1/orders
Authorization: Bearer <token>
Content-Type: application/json

{
  "symbol": "PETR4",
  "quantity": 100,
  "price": 25.50,
  "side": "BUY",
  "orderType": "LIMIT",
  "timeInForce": "GTC"
}

# Response:
{
  "orderId": "550e8400-e29b-41d4-a716-446655440000",
  "symbol": "PETR4",
  "side": "BUY",
  "quantity": 100,
  "price": 25.50,
  "status": "Accepted",
  "createdAt": "2026-07-15T23:52:00Z",
  "rejectionReason": null
}
```

```bash
GET /api/v1/orders
Authorization: Bearer <token>

# Response:
{
  "data": [
    { "orderId": "...", "symbol": "PETR4", ... },
    { "orderId": "...", "symbol": "VALE3", ... }
  ],
  "pageNumber": 1,
  "pageSize": 50,
  "totalCount": 2
}
```

### Health Checks

```bash
GET /api/v1/health

# Response:
{
  "status": "Healthy",
  "checks": {
    "PostgreSQL": "Healthy",
    "Redis": "Healthy"
  }
}
```

## Observabilidade

### Structured Logging

Logs estruturados com correlação via OpenTelemetry:

```
[2026-07-15 23:52:00] INF Received NewOrderSingle: ClOrdID=ORD123
  TraceId: 0144975ac6ed77b18ff787b8b067bd22
  SpanId: 3d5de17006805895
  Symbol: PETR4
  Quantity: 100
  Price: 25.50
```

### Métricas e Traces

- Activities criadas por NewOrderSingle e ExecutionReport
- Instrumentação SQL (queries com duração)
- Instrumentação AspNetCore (endpoints com latência)
- Console Exporter para desenvolvimento

## Licença

Este projeto está licenciado sob a licença MIT - veja o arquivo [LICENSE](LICENSE) para detalhes.

## Autor

Desenvolvido como amostra de arquitetura em mercados de capitais com protocolo FIX.
