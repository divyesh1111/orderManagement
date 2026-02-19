# Containerized E-Commerce Microservices Backend

A containerized distributed backend for a simplified e-commerce platform implemented using:

- ASP.NET Core (.NET 8)
- Entity Framework Core (async)
- PostgreSQL (database-per-service)
- RabbitMQ (event-based communication)
- Docker + Docker Compose

---

## Services

| Service | Responsibility | Port (Host → Container) |
|----------|----------------|--------------------------|
| ProductService | Product catalog, price, stock. Subscribes to OrderCreated to decrement stock. | 5001 → 8080 |
| CustomerService | Customer information. | 5002 → 8080 |
| OrderService | Creates and tracks orders. Validates customer and product existence via HTTP. Publishes OrderCreated event. | 5003 → 8080 |
| NotificationService | Subscribes to OrderCreated events and stores logs. | 5004 → 8080 |
| RabbitMQ | Message broker + management UI. | 5672, 15672 |
| Postgres DBs | One database per service. | Internal |

---

## Architecture Summary

- Database-per-service: each microservice has its own DbContext and its own PostgreSQL database.
- Synchronous communication (HTTP): OrderService validates customer and product existence using HttpClient.
- Asynchronous communication (RabbitMQ): OrderService publishes OrderCreated; ProductService updates stock; NotificationService stores logs.
- Eventual consistency: stock is updated asynchronously after the order is committed.

---

## Prerequisites

Required:

- Docker Desktop (Linux containers mode)
- Docker Compose (included with Docker Desktop)

Optional (only if you create/update migrations locally):

- .NET SDK 8
- dotnet-ef tool

---

## Run the System

From repository root:

```bash
docker compose up --build
```

Run in background:

```bash
docker compose up -d --build
```

Stop:

```bash
docker compose down
```

---

## Swagger URLs

- Product Service: http://localhost:5001/swagger
- Customer Service: http://localhost:5002/swagger
- Order Service: http://localhost:5003/swagger
- Notification Service: http://localhost:5004/swagger

---

## RabbitMQ Management UI

- URL: http://localhost:15672
- Username: guest
- Password: guest

---

## API Overview

### Product Service

- GET /products  
- GET /products/{id}  
- GET /products/{id}/exists  
- POST /products  
- PUT /products/{id}  
- DELETE /products/{id}  

### Customer Service

- GET /customers  
- GET /customers/{id}  
- GET /customers/{id}/exists  
- POST /customers  
- PUT /customers/{id}  
- DELETE /customers/{id}  

### Order Service

- GET /orders  
- GET /orders/{id}  
- POST /orders  

### Notification Service

- GET /notifications  

---

## End-to-End Test (Required Flow)

### 1. Create a customer (Customer Service)

POST /customers

```json
{
  "firstName": "Ali",
  "lastName": "Khan",
  "email": "ali@example.com"
}
```

### 2. Create a product with stock (Product Service)

POST /products

```json
{
  "name": "Keyboard",
  "price": 50,
  "stock": 10
}
```

### 3. Create an order (Order Service)

POST /orders

```json
{
  "customerId": "CUSTOMER_ID",
  "items": [
    { "productId": "PRODUCT_ID", "quantity": 2 }
  ]
}
```

### 4. Verify stock changed (Product Service)

GET /products/{PRODUCT_ID}

Expected result:

- Stock becomes 8  
- Eventual consistency applies; allow a short delay  

### 5. Verify notification log (Notification Service)

GET /notifications

Expected result:

- A log entry referencing the created order  

---

## Migrations (Local Development)

Install tool:

```bash
dotnet tool install --global dotnet-ef
```

Create migrations (independent per service):

```bash
dotnet ef migrations add InitialCreate -p src/ProductService/ProductService.Api/ProductService.Api.csproj -o Migrations
dotnet ef migrations add InitialCreate -p src/CustomerService/CustomerService.Api/CustomerService.Api.csproj -o Migrations
dotnet ef migrations add InitialCreate -p src/OrderService/OrderService.Api/OrderService.Api.csproj -o Migrations
dotnet ef migrations add InitialCreate -p src/NotificationService/NotificationService.Api/NotificationService.Api.csproj -o Migrations
```

Databases are migrated automatically on startup inside each service using:

```
Database.MigrateAsync()
```

---

## Troubleshooting

Check status:

```bash
docker compose ps
```

Check logs:

```bash
docker compose logs --tail 200 product-service
docker compose logs --tail 200 customer-service
docker compose logs --tail 200 order-service
docker compose logs --tail 200 notification-service
```

Clean rebuild:

```bash
docker compose down
docker compose build --no-cache
docker compose up --build
```

---

## Rubric Mapping

- Architecture correctness: microservices with clear ownership and no shared database
- Database-per-service: separate Postgres DB per service + separate DbContext
- EF Core async usage: async EF Core methods across services
- Docker & Compose: Dockerfile per service + docker-compose orchestration
- Synchronous validation: OrderService validates customer and product via HttpClient
- Event-based communication: OrderCreated published by OrderService; ProductService updates stock; NotificationService logs event
- Documentation: README + IEEE report + architecture diagram
- Code organization: consistent folder layout, DTOs, endpoints, messaging, shared contracts

---

# .editorconfig

```ini
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
indent_style = space
indent_size = 4
trim_trailing_whitespace = true

[*.cs]
dotnet_sort_system_directives_first = true
csharp_new_line_before_open_brace = all
csharp_indent_case_contents = true
csharp_indent_switch_labels = true

[*.{yml,yaml,json,md}]
indent_size = 2
```

---

# docs/CODE_FORMAT.md

```markdown
# Code Format and Organization Standard

This document defines the project code structure and formatting conventions.

## Folder Structure (Per Service)

Each microservice project should follow:

- Data/
  - DbContext and EF configuration
- Entities/
  - EF Core entity classes
- Dtos/
  - Request/response records
- Endpoints/
  - Minimal API endpoint mappings
- Messaging/ (only for services that publish/consume events)
  - RabbitMQ options and publisher/subscriber
- Http/ (OrderService only)
  - Typed HttpClient wrappers and options

## Naming Conventions

- Projects: XxxService.Api
- Namespaces: XxxService.Api.<FolderName>
- Entities: PascalCase, primary key as Guid Id
- DTOs: XxxCreateDto, XxxUpdateDto, XxxReadDto
- Async methods: suffix Async (ExistsAsync, GetAsync)
- RabbitMQ naming:
  - Exchange: orders.exchange
  - Routing key: order.created
  - Queues: product.ordercreated, notification.ordercreated

## Minimal API Layout

Program.cs should contain:

- DI registrations
- Swagger setup
- Database.MigrateAsync() execution at startup
- Map*Endpoints() call

Route mappings should be defined in Endpoints/*Endpoints.cs

## EF Core Async Rule

Use async EF Core methods:

- ToListAsync
- SingleOrDefaultAsync
- AnyAsync
- FindAsync
- AddAsync
- SaveChangesAsync

## No Comments Requirement

Do not add:

- // line comments
- /* block comments */
- XML doc comments

Use clear naming and structure instead of comments.

## Formatting

Use dotnet format (optional):

```bash
dotnet format
```

.editorconfig in repo root defines whitespace and indentation rules.
```
