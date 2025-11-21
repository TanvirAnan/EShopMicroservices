# EShop Microservices (.NET8)

A microservices learning solution built with .NET8 and C#12. It includes REST and gRPC services, PostgreSQL (via Marten), SQLite (via EF Core), Redis cache, Health Checks, and Docker Compose for local orchestration.

---

## Architecture overview
- API Gateway: none (each service is accessed directly in dev)
- Services
 - Catalog.API (HTTP/REST)
 - Basket.API (HTTP/REST)
 - Discount.Grpc (gRPC)
 - Ordering.* (API/Application/Domain/Infrastructure layers)
- Data stores
 - PostgreSQL: Catalog, Basket (via Marten)
 - SQLite: Discount (EF Core)
 - Redis: Distributed cache for Basket
- Communication
 - Synchronous HTTP (between client and REST services)
 - Synchronous gRPC (Basket -> Discount)
- Cross‑cutting
 - MediatR + CQRS, pipeline behaviors (logging/validation)
 - Mapster for object mapping
 - FluentValidation for command validation
 - Centralized exception handling (CustomExceptionHandler)
 - Health checks (DB/Redis)

---

## Services and responsibilities
- Catalog.API
 - Product CRUD and queries over HTTP
 - Persists to PostgreSQL using Marten
 - Health checks for PostgreSQL
- Basket.API
 - Shopping cart operations (get/store/delete) over HTTP
 - Persists to PostgreSQL using Marten, cached with Redis
 - Calls Discount.Grpc to apply coupon amounts when storing basket
 - Health checks for PostgreSQL and Redis
- Discount.Grpc
 - gRPC service to manage and retrieve discount coupons
 - Persists to SQLite via EF Core
 - Seeds two coupons at startup
- Ordering.*
 - Foundations for ordering domain with layered architecture (Domain, Application, Infrastructure, API)

---

## Key flows
- Apply discount on checkout/store basket
1) Client calls Basket.API StoreBasket
2) Basket.API iterates items and calls Discount.Grpc `GetDiscount` per item
3) Basket.API deducts returned coupon amount from item price
4) Basket.API stores the cart via Marten and updates cache

---

## Validation, logging, and exceptions
- Validation
 - FluentValidation rules (example: `StoreBasketCommandValidator`)
 - `ValidationBehavior<TRequest,TResponse>` enforces rules for commands (types implementing `ICommand<TResponse>`) before handlers run
- Logging
 - `LoggingBehavior<TRequest,TResponse>` logs request pipeline details
- Exceptions
 - Centralized handler `CustomExceptionHandler` formats error responses

Note: queries (`IQuery<T>`) are not validated by the current validation behavior. Add a query-specific behavior if desired.

---

## gRPC (Discount.Grpc)
- Contract: `Services/Discount/Discount.Grpc/Protos/discount.proto`
 - Methods: `GetDiscount`, `CreateDiscount`, `UpdateDiscount`, `DeleteDiscount`
 - Messages: `GetDiscountRequest { productName }`, `CouponModel { id, productName, description, amount }`
- Server
 - ASP.NET Core gRPC on HTTP/2
 - Uses EF Core (SQLite) and migrates/seeds on startup
- Client (Basket.API)
 - Uses `GrpcClientFactory` to create `DiscountProtoServiceClient`
 - Calls `GetDiscount` to retrieve discount for each cart item

HTTP/2 and TLS
- In Docker, Discount.Grpc runs HTTP (plaintext) on port8080 and is mapped to host6002
- Basket.API must call Discount.Grpc over HTTP when in Docker (no TLS)
- If you use HTTPS locally, ensure Discount.Grpc is started with HTTPS and trusted certificates

---

## Configuration
- Basket.API `appsettings.json`
 - `ConnectionStrings:Database` – Postgres connection for Marten (BasketDb)
 - `ConnectionStrings:Redis` – Redis endpoint
 - `GrpcSettings:DiscountUrl` – Base address for Discount.Grpc
- Discount.Grpc `appsettings.json`
 - `ConnectionStrings:Database` – `Data Source=discountdb` (SQLite file)
 - Kestrel is configured for HTTP/2
- Catalog.API `appsettings.json`
 - PostgreSQL connection for Marten

Environment overrides in Docker Compose:
- Basket.API: set `GrpcSettings__DiscountUrl=http://discount.grpc:8080`
- Basket.API: `ConnectionStrings__Redis=distributedcache:6379`
- Catalog.API/Basket.API: Postgres connections point to container hosts (`catalogdb`, `basketdb`)

---

## Packages per service
- Discount.Grpc
 - Grpc.AspNetCore2.71.0, Mapster7.4.0
 - Microsoft.EntityFrameworkCore.Sqlite/Tools9.0.10
- Basket.API
 - Carter8.0.0, Grpc.AspNetCore2.49.0
 - Marten8.14.0, Scrutor6.1.0
 - Microsoft.Extensions.Caching.StackExchangeRedis8.0.21
 - AspNetCore.HealthChecks: NpgSql/Redis/UI.Client9.0.0
- Catalog.API
 - Carter8.0.0, Marten8.13.3, Weasel.Postgresql8.2.0
 - AspNetCore.HealthChecks: NpgSql/UI.Client9.0.0
- BuildingBlocks
 - MediatR13.1.0, Mapster7.4.0
 - FluentValidation12.1.0 (+ ASP.NET Core + DI extensions)
- Ordering.Domain
 - MediatR13.1.0

---

## Run locally (without Docker)
- Databases
 - Postgres for Catalog and Basket (adjust connection strings in appsettings)
 - Redis at `localhost:6379`
 - SQLite file for Discount: `discountdb`
- Start projects
 - Discount.Grpc: profile exposes `http://localhost:5002` (and optional `https://localhost:5052`)
 - Catalog.API and Basket.API: standard ASP.NET URLs
- Basket.API gRPC client
 - Set `GrpcSettings:DiscountUrl` to the Discount URL you run (HTTP or HTTPS)

---

## Run with Docker
From the `Src` folder:
- `docker-compose -f ../docker-compose.yml -f ../docker-compose.override.yml up -d --build`

Exposed ports (host):
- Catalog.API: http://localhost:6000
- Basket.API: http://localhost:6001
- Discount.Grpc: http://localhost:6002 (HTTP/2 plaintext)

Important
- Basket.API must call Discount.Grpc via HTTP inside Docker
 - Set `GrpcSettings__DiscountUrl=http://discount.grpc:8080` in `docker-compose.override.yml`
- Health checks
 - Basket.API: http://localhost:6001/health

---

## Data and migrations
- Discount.Grpc (EF Core + SQLite)
 - Applies migrations and seeds two coupons at startup (IPhone X:150, Samsung10:100)
 - If you see no data locally, ensure migration is awaited and delete the `discountdb` file to re-seed
- Basket.API (Marten + PostgreSQL)
 - Uses `ShoppingCart` with `UserName` as identity

---

## Testing
- Postman (gRPC)
 - Method: `discount.DiscountProtoService/GetDiscount`
 - Target: `http://localhost:6002` (Docker) or local Discount URL
 - Body: `{ "productName": "IPhone X" }`
- Basket flow
 - POST StoreBasket, then GET basket to verify discounted prices were applied

---

## Troubleshooting
- SSL error calling gRPC
 - Using `https://` to call a server that runs only HTTP results in TLS handshake errors
 - In Docker, use `http://discount.grpc:8080` from Basket.API
- Empty discounts
 - Ensure migrations ran and seed executed; delete `discountdb` if needed
- Product name mismatch
 - Discount lookups are case-insensitive with trimming if you applied the latest changes

---

## Contributing
- Standard PR workflow. Keep services independent, validate locally, and update Docker Compose ports/env as needed.

## License
- MIT (or add your license).
