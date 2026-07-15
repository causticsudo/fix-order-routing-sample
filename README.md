# FIX Order Routing System

[![Build and Test](https://github.com/your-org/fix-order-routing-sample/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/your-org/fix-order-routing-sample/actions/workflows/build-and-test.yml)
[![Release](https://github.com/your-org/fix-order-routing-sample/actions/workflows/release.yml/badge.svg)](https://github.com/your-org/fix-order-routing-sample/actions/workflows/release.yml)
![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat)
![License](https://img.shields.io/badge/license-MIT-green)

A high-criticality order routing system for capital markets using the **FIX 4.4 protocol**. Built with a **Vertical Sliced Architecture**, **DDD principles**, and **CQRS pattern** for maximum reliability, performance, and maintainability.

## Architecture

### System Components

```
┌─────────────────────────────────────────────────────────────────┐
│                      Order Generator                             │
│  (REST API + React Frontend)                                    │
├─────────────────────────────────────────────────────────────────┤
│  - MediatR + CQRS Handlers                                      │
│  - Keycloak Authentication                                      │
│  - Redis Token Cache + In-Memory Cache                          │
│  - PostgreSQL Persistence (Repository Pattern)                  │
│  - OpenTelemetry → Jaeger Traces                                │
└──────────────────┬───────────────────────────────────────────────┘
                   │ FIX 4.4 Protocol
                   │ (Client/Initiator)
                   ↓
┌─────────────────────────────────────────────────────────────────┐
│                   Order Accumulator                              │
│  (Worker Service)                                               │
├─────────────────────────────────────────────────────────────────┤
│  - FIX Server (Acceptor)                                        │
│  - Event Store (PostgreSQL)                                     │
│  - Event Replay on Startup                                      │
│  - Exposure Calculation (in-memory)                             │
│  - Outbox Pattern for Reliability                               │
│  - OpenTelemetry → Jaeger Traces                                │
└─────────────────────────────────────────────────────────────────┘
```

### Infrastructure

- **PostgreSQL 15**: Primary datastore (Orders, Events, Audit)
- **Redis 7**: Distributed token cache
- **Keycloak**: Identity and Access Management
- **Jaeger**: Distributed tracing (traces, metrics, logs)
- **Docker Compose**: Local development environment

## Quick Start

### Prerequisites

- Docker & Docker Compose
- .NET 8 SDK (optional, for local development)
- Git

### Local Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-org/fix-order-routing-sample.git
   cd fix-order-routing-sample
   ```

2. **Start all services with Docker Compose**
   ```bash
   cd .docker
   docker-compose up
   ```
   This will:
   - Build OrderGenerator.Api and OrderAccumulator.Worker from source
   - Start PostgreSQL (creates `order_generator` database automatically)
   - Start Redis, Jaeger, and Keycloak
   - Initialize API and Worker services

3. **Verify services are running**
   ```bash
   # OrderGenerator API: http://localhost:5000
   # OrderAccumulator Worker: localhost:9000 (FIX)
   # Jaeger UI: http://localhost:16686
   # Keycloak Admin: http://localhost:8080/admin (admin/admin)
   # PostgreSQL: localhost:5432
   # Redis: localhost:6379
   ```

4. **Test the API**
   
   **Generate a debug JWT token:**
   ```bash
   TOKEN=$(curl -s -X POST http://localhost:5000/api/v1/token/debug | jq -r .token)
   echo $TOKEN
   ```

   **Submit an order:**
   ```bash
   curl -X POST http://localhost:5000/api/v1/orders \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d '{
       "symbol": "PETR4",
       "side": "BUY",
       "quantity": 100,
       "price": 25.50
     }'
   ```
   Expected response (201 Created):
   ```json
   {
     "orderId": "b370c51a-92d6-4302-a5ac-a272054f438d",
     "symbol": "PETR4",
     "side": "BUY",
     "quantity": 100,
     "price": 25.5,
     "status": "Created",
     "createdAt": "2026-07-15T05:35:53.1852066Z"
   }
   ```

   **Valid symbols:** PETR4, VALE3, VIIA4  
   **Valid sides:** BUY, SELL  
   **Quantity range:** 1 to 99,999  
   **Price range:** 0.01 to 999.99 (multiples of 0.01)

5. **Configure Keycloak** (optional, for production)
   - Navigate to [Keycloak Admin Console](http://localhost:8080/admin)
   - Login with: admin / admin
   - Create realm: `fix-order-routing`
   - Create client: `fix-order-generator`
   - Create users and assign roles

6. **Stop all services**
   ```bash
   cd .docker
   docker-compose down
   ```

### Building from Source

```bash
dotnet restore FixOrderRouting.sln
dotnet build FixOrderRouting.sln --configuration Release
dotnet test FixOrderRouting.sln
```

## Features

### Order Generator

- **REST API** for order submission
- **MediatR + CQRS** for command handling
- **FluentValidation** for input validation
- **Keycloak Authentication** with Redis token cache
- **React Frontend** for order submission
- **OpenTelemetry Tracing** for observability

**Endpoints:**
- `POST /api/v1/orders` — Submit new order
- `GET /api/v1/orders/{orderId}` — Get order status
- `GET /api/v1/health` — Health check

### Order Accumulator

- **FIX 4.4 Server** (QuickFix/N)
- **Exposure Calculation** — Real-time risk management
- **OpenTelemetry Tracing** for observability

**Business Logic:**
- Accept/Reject orders based on `R$ 100M` exposure limit per symbol
- Calculate exposure: `Σ(buy_price × buy_qty) - Σ(sell_price × sell_qty)`
- Respond with `ExecutionReport` (ExecType: `New` or `Rejected`)

## 🧪 Testing

```bash
# Unit tests
dotnet test tests/OrderGenerator.UnitTests
dotnet test tests/OrderAccumulator.UnitTests

# Integration tests
dotnet test tests/OrderGenerator.IntegrationTests
dotnet test tests/OrderAccumulator.IntegrationTests

# All tests
dotnet test FixOrderRouting.sln
```

## Observability

### Jaeger Dashboard

Navigate to [Jaeger UI](http://localhost:16686) to:
- View distributed traces end-to-end
- Search by service, operation, tag
- Analyze latency bottlenecks
- Monitor error rates

### Logs

- **Serilog** structured logging
- **JSON format** for easy parsing
- **Correlation IDs** for trace linking

Example log query:
```
SELECT * FROM logs WHERE correlation_id = '...'
```

## Security

- **Keycloak** for centralized authentication
- **Bearer tokens** with Redis cache
- **HTTPS only** in production
- **Rate limiting** on API endpoints
- **SQL injection prevention** via EF Core
- **CORS** properly configured
- **Sensitive data** masked in logs

## CI/CD Actions

### Build & Test (`build-and-test.yml`)
Runs on every push and pull request:
1. ✅ Restore dependencies
2. ✅ Build solution (Release config)
3. ✅ Run unit tests (`OrderGenerator.UnitTests`, `OrderAccumulator.UnitTests`)
4. ✅ Run integration tests (`OrderGenerator.IntegrationTests`, `OrderAccumulator.IntegrationTests`)
5. ℹ️ SonarQube analysis (commented out, enable as needed)

### Release (`release.yml`)
Runs on push to `main` branch:
1. ✅ Build solution in Release config
2. ✅ Publish standalone executables:
   - `OrderGenerator.Api` → tar.gz
   - `OrderAccumulator.Worker` → tar.gz
3. ✅ Auto-generate semantic tag: `v{YYYYMMDD}-{commit-hash}`
4. ✅ Create GitHub release with:
   - Published binaries (ready to run)
   - `docker-compose.yml` (full local environment)
   - Dockerfiles for both services
   - `.env` example configuration

**For Interviewers:** Download the GitHub release to get everything needed to run the system locally!

## Documentation
- [C4 Diagrams](docs/C4/) — System architecture visualizations

> This repository is a codesh challenge.

## License

MIT License — See [LICENSE](LICENSE).
