# FIX Order Routing System

[![Build and Test](https://github.com/your-org/fix-order-routing-sample/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/your-org/fix-order-routing-sample/actions/workflows/build-and-test.yml)
[![Release](https://github.com/your-org/fix-order-routing-sample/actions/workflows/release.yml/badge.svg)](https://github.com/your-org/fix-order-routing-sample/actions/workflows/release.yml)
![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat)
![License](https://img.shields.io/badge/license-MIT-green)

A high-criticality order routing system for capital markets using the **FIX 4.4 protocol**. Built with a **Vertical Sliced Architecture**, **DDD principles**, and **CQRS pattern** for maximum reliability, performance, and maintainability.

## 🏗️ Architecture

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

## 🚀 Quick Start

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

2. **Start infrastructure with Docker Compose**
   ```bash
   docker-compose -f .docker/docker-compose.yml up -d
   ```

3. **Verify services are running**
   ```bash
   # OrderGenerator API: http://localhost:5000
   # OrderAccumulator Worker: localhost:9000 (FIX)
   # Jaeger UI: http://localhost:16686
   # Keycloak Admin: http://localhost:8080/admin (admin/admin)
   # PostgreSQL: localhost:5432
   # Redis: localhost:6379
   ```

4. **Configure Keycloak** (first time only)
   - Navigate to [Keycloak Admin Console](http://localhost:8080/admin)
   - Create realm: `fix-order-routing`
   - Create client: `fix-order-generator`
   - Create users and assign roles

5. **Access the application**
   - Order Generator UI: http://localhost:3000 (frontend)
   - Jaeger Traces: http://localhost:16686

### Building from Source

```bash
dotnet restore FixOrderRouting.sln
dotnet build FixOrderRouting.sln --configuration Release
dotnet test FixOrderRouting.sln
```

## 📋 Features

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
- **Event Sourcing** — Complete audit trail
- **Exposure Calculation** — Real-time risk management
- **Event Replay** — Crash recovery with zero data loss
- **Outbox Pattern** — Guaranteed message delivery
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

## 📊 Observability

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

## 🔐 Security

- **Keycloak** for centralized authentication
- **Bearer tokens** with Redis cache
- **HTTPS only** in production
- **Rate limiting** on API endpoints
- **SQL injection prevention** via EF Core
- **CORS** properly configured
- **Sensitive data** masked in logs

## 📈 CI/CD Pipeline

### Build & Test (`build-and-test.yml`)
1. ✅ Build solution
2. ✅ Run unit tests
3. ✅ Run integration tests (with Testcontainers)
4. ✅ SonarQube analysis (if configured)

### Release (`release.yml`)
1. ✅ Publish artifacts
2. ✅ Build Docker images
3. ✅ Push to registry (ghcr.io)
4. ✅ Create GitHub releases

### Branch Strategy (GitFlow)

```
main (release ready)
  ↑
develop (integration branch)
  ↑
feature/* (individual features)
  ├── feature/order-generator-backend
  └── feature/order-accumulator
```

## 📚 Documentation

- [Setup Guide](docs/SETUP.md) — Local environment setup
- [Architecture](docs/ARCHITECTURE.md) — Design decisions
- [API Documentation](docs/API.md) — REST API specs
- [Contributing](docs/CONTRIBUTING.md) — Development guidelines
- [C4 Diagrams](docs/C4/) — System architecture visualizations

## 📄 License

MIT License — See [LICENSE](LICENSE).
