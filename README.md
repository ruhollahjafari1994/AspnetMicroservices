# Aspnet Microservices Store

A sample e-commerce platform built with independently deployable .NET services. It demonstrates service-to-service communication, containerized development, and an orchestrated checkout and cancellation workflow.

> **Project status:** This is a learning and demonstration project. Review the security and operational notes before adapting it for production.

![.NET](https://img.shields.io/badge/.NET-microservices-512BD4?logo=dotnet&logoColor=white)
![Docker Compose](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)
![RabbitMQ](https://img.shields.io/badge/Messaging-RabbitMQ-FF6600?logo=rabbitmq&logoColor=white)
![Workflow](https://img.shields.io/badge/Workflow-Wolverine-6B46C1)

## What this project demonstrates

- Service-oriented e-commerce domains: catalog, basket, discounts, and orders
- Synchronous service calls with HTTP and gRPC
- Asynchronous messaging with RabbitMQ and Wolverine
- Checkout idempotency using a stable event ID and a unique database index
- An orchestrated Saga that can compensate a checkout by restoring the basket
- Retry and circuit-breaker policies with Polly for transient failures
- Docker Compose development environment with databases, message broker, gateway, and web UI
- Centralized logging and health-check dashboards

## Architecture

## Local setup and configuration

Docker Compose reads local development credentials from `.env` at the repository root. Start with `Copy-Item .env.example .env` (PowerShell) or `cp .env.example .env` (Linux/macOS), replace every `replace-with-...` value with a strong local-only value, then run `docker compose -f src/docker-compose.yml -f src/docker-compose.override.yml up --build`. Do not commit `.env`.

The required local variables are `MSSQL_SA_PASSWORD`, `DISCOUNT_DB_USER`, `DISCOUNT_DB_PASSWORD`, `RABBITMQ_USER`, `RABBITMQ_PASSWORD`, `WEB_OIDC_CLIENT_SECRET`, `BASKET_OIDC_CLIENT_SECRET`, `PGADMIN_EMAIL`, and `PGADMIN_PASSWORD`. The bootstrap admin variables are optional and only seed a local development account. Compose uses an HTTP issuer and disables HTTPS metadata only for its Development containers; production must configure HTTPS identity issuer/authority values and inject database, broker, and OIDC secrets from a secret manager. Never deploy the development issuer or example credentials.

All .NET projects target .NET 8 and Dockerfiles use the .NET 8 SDK/runtime images. Use a .NET 8 SDK installation and a Visual Studio release that supports .NET 8.

Testcontainers integration tests require a running Docker engine. Run the full suite with `dotnet test` from the repository root.

```mermaid
flowchart LR
    Customer[Customer] --> Web[Storefront<br/>Razor Pages]
    Web -->|OIDC / PKCE sign-in| Identity[Identity.API]
    Web --> Gateway[Ocelot API Gateway]
    Gateway --> Catalog[Catalog API]
    Gateway --> Basket[Basket API]
    Gateway --> Ordering[Ordering API]
    Basket -. gRPC discount lookup .-> Discount[Discount gRPC]
    Basket -->|Checkout event| Rabbit[(RabbitMQ)]
    Rabbit -->|Checkout event| Ordering
    Ordering -->|Saga commands| Rabbit
    Rabbit -->|Clear / restore basket| Basket
    Basket -->|Step acknowledgements| Rabbit
    Rabbit -->|Saga events| Ordering
    Catalog --> Mongo[(MongoDB)]
    Basket --> Redis[(Redis)]
    Discount --> PostgreSQL[(PostgreSQL)]
    Ordering --> SQL[(SQL Server)]
    Identity --> IdentityDB[(IdentityDb / SQL Server)]
```

### Services and infrastructure

| Component | Responsibility | Main technology |
|---|---|---|
| `Catalog.API` | Product catalog and search | ASP.NET Core, MongoDB |
| `Basket.API` | Shopping basket and checkout messages | ASP.NET Core, Redis, Wolverine |
| `Discount.Grpc` | Discount lookup for basket pricing | gRPC, PostgreSQL, Dapper |
| `Discount.API` | Discount HTTP API | ASP.NET Core, PostgreSQL |
| `Ordering.API` | Orders, checkout processing, and cancellation Saga | ASP.NET Core, EF Core, SQL Server, Wolverine |
| `Identity.API` | User accounts, roles, and OIDC token issuance | ASP.NET Core Identity, OpenIddict, EF Core, SQL Server |
| `OcelotApiGw` | Routes storefront API requests | Ocelot |
| `Shopping.Aggregator` | Aggregates responses for storefront scenarios | ASP.NET Core, `HttpClientFactory` |
| `AspnetRunBasics` | Storefront UI | ASP.NET Core Razor Pages |
| `WebStatus` | Health-check dashboard | ASP.NET Core Health Checks UI |
| RabbitMQ | Inter-service message broker | RabbitMQ Management |
| Elasticsearch / Kibana | Centralized logs and log exploration | Elastic Stack |

## Authentication and authorization

The storefront uses OpenID Connect Authorization Code flow with PKCE. The Identity service stores users and roles with ASP.NET Core Identity in the separate SQL Server database IdentityDb and issues JWT access tokens through OpenIddict. New registrations receive the Customer role. The storefront keeps an authentication cookie and forwards its access token to the APIs.

- Identity endpoints: http://localhost:8009
- Storefront login and account registration: http://localhost:8006/login
- Catalog and discount reads remain public; write operations require the Administrator role.
- Basket and order endpoints require an authenticated token and check that the requested user matches the token subject/name.
- The order list and cancellation are restricted to the owning customer; direct order creation and editing are administrator-only.
- Access tokens are validated by the APIs using Identity OpenID Connect discovery/JWKS metadata.

To provision an administrator for local development, set these variables before starting Compose (PowerShell example):

~~~powershell
$env:IDENTITY_BOOTSTRAP_ADMIN_USERNAME = "store-admin"
$env:IDENTITY_BOOTSTRAP_ADMIN_PASSWORD = "Use-A-Unique-Strong-Password"
$env:IDENTITY_BOOTSTRAP_ADMIN_EMAIL = "admin@example.test"
docker compose -f src/docker-compose.yml -f src/docker-compose.override.yml up -d --build
~~~

Leave the variables unset to disable administrator bootstrap; users can still register as customers. The Compose client secrets, HTTP issuer, development signing certificates, and SQL credentials are for local development only. For a deployed environment, use HTTPS, supply unique secrets through a secret manager, and configure persistent signing/encryption certificates.

## Checkout and cancellation workflow

Checkout spans Basket, Ordering, RabbitMQ, Redis, and SQL Server. The flow uses an orchestrated Saga so each step and its acknowledgement can be tracked.

```mermaid
sequenceDiagram
    participant UI as Storefront
    participant B as Basket API
    participant Q as RabbitMQ / Wolverine
    participant O as Ordering API / Saga
    participant DB as SQL Server
    participant R as Redis
    UI->>B: Checkout (stable CheckoutId)
    B->>Q: BasketCheckoutEvent + basket snapshot
    Q->>O: BasketCheckoutEvent
    O->>DB: Find or create order by CheckoutEventId
    O->>DB: Persist Saga and snapshot
    O->>Q: ClearBasketForOrder
    Q->>B: ClearBasketForOrder
    B->>R: Delete basket
    B->>Q: BasketClearedForOrder
    Q->>O: BasketClearedForOrder
    O->>DB: Mark Saga Confirmed
```

### How idempotency works

1. The checkout page creates a stable `CheckoutId` and posts it with the form.
2. Ordering looks for an order with that `CheckoutEventId` before creating one.
3. SQL Server enforces a unique filtered index on `Orders.CheckoutEventId`. This is the final protection when concurrent deliveries pass the initial lookup at the same time.
4. If a concurrent insert wins, the handler clears the EF Core change tracker and reads the winning order. The Saga then uses that order ID instead of creating a duplicate.

### How cancellation compensates a checkout

The order cancellation endpoint sends `CancelOrderSaga` and returns `202 Accepted`. The Saga changes state and sends `RestoreBasketForOrder` with the basket snapshot saved during checkout. Basket recreates the Redis basket and acknowledges the restore. Only after that acknowledgement does Ordering delete the order and mark the Saga as cancelled.

If cancellation arrives before the Saga start message, Ordering schedules another cancellation attempt after one second, up to 15 times. If no Saga appears, the order is left intact and a warning is logged. Legacy orders without a checkout ID cannot be safely compensated and receive `409 Conflict`.

### Concurrency and delivery guarantees

- Checkout messages are grouped by event ID and processed in message partitions.
- Saga state uses EF Core optimistic concurrency through its version field. Wolverine retries a `SagaConcurrencyException` with short cooldowns.
- Durable Inbox and Outbox are enabled for Wolverine message processing and outgoing endpoints.
- The unique checkout index protects against duplicate order creation even if messages are delivered more than once.
- Basket operations are replay-safe for this workflow: clearing a basket more than once is harmless, and restoring the same snapshot overwrites the basket with the same contents.

These mechanisms do not create a single transaction across SQL Server, Redis, and RabbitMQ. Cross-service consistency is achieved through durable messages, Saga state, acknowledgements, and compensation.

## Resilience and retry policies

Retries are scoped to the failure they can safely recover from:

| Location | Policy | Purpose |
|---|---|---|
| Ordering checkout handler | Polly: 3 retries after 200 ms, 400 ms, and 800 ms | Retries transient SQL exceptions, including SQL exceptions wrapped by EF Core. Clears the EF change tracker before retrying. |
| Ordering startup migration | Polly: 5 retries with 2, 4, 8, 16, and 32 second delays | Gives the SQL Server container time to become available during startup. |
| Saga persistence | Wolverine: retries concurrency conflicts after 100 ms, 250 ms, and 500 ms | Re-runs message handling when optimistic concurrency rejects the Saga update. |
| Storefront and Aggregator HTTP clients | Polly retry and circuit breaker | Handles temporary downstream HTTP failures and stops repeated calls while a dependency is unhealthy. |

The checkout Polly policy does not retry unique-key violations. Those are handled by the idempotency path, which reads the order created by the competing request. Wolverine owns the Saga concurrency retry because the conflict may occur during Saga persistence after the handler has returned.

## Quick start

### Prerequisites

- Docker Desktop with Docker Compose
- .NET SDK for local builds and tests

### Start the full stack

Run this from the repository root:

```powershell
docker compose -f src/docker-compose.yml -f src/docker-compose.override.yml up -d --build
```

Check container status and follow the workflow service logs:

```powershell
docker compose -f src/docker-compose.yml -f src/docker-compose.override.yml ps
docker compose -f src/docker-compose.yml -f src/docker-compose.override.yml logs -f ordering.api basket.api
```

Stop the stack:

```powershell
docker compose -f src/docker-compose.yml -f src/docker-compose.override.yml down
```

Compose volumes preserve database and log data after containers stop. `docker compose down -v` also removes those volumes and their data.

## Local endpoints

| Component | URL |
|---|---|
| Storefront | <http://localhost:8006> |
| Catalog Swagger | <http://localhost:8000/swagger> |
| Basket Swagger | <http://localhost:8001/swagger> |
| Discount Swagger | <http://localhost:8002/swagger> |
| Ordering Swagger | <http://localhost:8004/swagger> |
| Ocelot API Gateway | <http://localhost:8010> |
| RabbitMQ Management | <http://localhost:15672> (`guest` / `guest`) |
| Elasticsearch | <http://localhost:9200> |
| Kibana | <http://localhost:5601> |
| Web Status | <http://localhost:8007> |
| pgAdmin | <http://localhost:5050> (credentials are configured in `src/.env`) |
| Portainer | <http://localhost:9000> |

The order page is available at `/Order` in the storefront. Basket and Ordering health checks are exposed at `/hc` on their respective API ports.

## Run tests

The Ordering concurrency test uses a temporary SQLite database; it does not require Docker or an external SQL Server instance.

```powershell
dotnet test src/Services/Ordering/Ordering.UnitTests/Ordering.UnitTests.csproj
```

Run all tests discovered by the solution:

```powershell
dotnet test src/aspnetrun-microservices.sln
```

The concurrency test races two inserts with the same checkout ID and verifies that the unique index allows only one order to persist.

## Repository layout

```text
src/
├── ApiGateways/
│   ├── OcelotApiGw/                 API routing
│   └── Shopping.Aggregator/         Aggregated API responses
├── BuildingBlocks/
│   ├── EventBus.Messages/           Event and workflow message contracts
│   └── Common.Logging/              Shared logging setup
├── Services/
│   ├── Basket/Basket.API/           Basket API, Redis, workflow handlers
│   ├── Catalog/Catalog.API/         Product catalog and MongoDB
│   ├── Discount/Discount.Grpc/      gRPC discount service
│   └── Ordering/
│       ├── Ordering.API/            API, Wolverine setup, workflow handlers
│       ├── Ordering.Application/    Commands, queries, application logic
│       ├── Ordering.Domain/         Order domain model
│       ├── Ordering.Infrastructure/ EF Core, SQL Server, checkout Saga
│       └── Ordering.UnitTests/      Checkout concurrency tests
└── WebApps/
    ├── AspnetRunBasics/             Razor Pages storefront
    └── WebStatus/                   Health dashboard
```

## Configuration and security notes

Development connection strings, ports, and service dependencies are defined in [`src/docker-compose.override.yml`](src/docker-compose.override.yml). The compose configuration contains sample credentials intended for local development; replace them and move secrets to a secure secret store before deploying anywhere shared or public.

Raw card number, expiration, and CVV fields have been removed from the checkout UI, API contracts, integration event, order entity, and order query models. The SQL migration `20261017160000_RemoveRawPaymentData` drops the legacy columns and their stored values. Checkout currently supports cash on delivery; online card payments remain unavailable until a provider hosted/tokenized flow and server side payment verification are integrated.

Local Compose credentials are read from `src/.env`. Copy `src/.env.example` to `src/.env` and fill in unique local values; `.env` is ignored by Git. For production, inject these values from a secrets manager rather than committing them or baking them into images.

Basket uses its own `BasketMessagingDb` connection for Wolverine message durability, separated from Ordering's `OrderDb`. Basket data itself remains in Redis. SQL Server and RabbitMQ delivery can be made durable through Wolverine's inbox/outbox, but the Redis basket and SQL Server message store do not share a distributed transaction: basket clearing/restoration remains an eventual workflow and can lag during outages. Wolverine creates the required message schema in the database selected by its connection string; the database itself must be available. See the official [Wolverine SQL Server persistence guide](https://wolverinefx.net/guide/durability/sqlserver) and [resource management guide](https://wolverinefx.net/guide/durability/managing).

## Contributing

Issues and pull requests that improve the examples, documentation, or reliability are welcome. For a change to checkout behavior, include tests for idempotency, concurrency, or compensation as appropriate.

---

If this repository helps you understand .NET microservices, consider giving it a star.
