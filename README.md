# Full-Stack Containerized E-Commerce Microservices System (Blazor + API Gateway)

This repository contains a containerized microservices-based e-commerce backend extended with a Blazor frontend. The solution demonstrates end-to-end integration through an API Gateway, database-per-service, asynchronous messaging with RabbitMQ, and DTO-based API design.

## Tech Stack

- ASP.NET Core (.NET 8)
- Entity Framework Core (async)
- PostgreSQL (database-per-service)
- RabbitMQ (event-driven messaging)
- YARP (API Gateway)
- Blazor Server (frontend)
- Docker and Docker Compose

## Services

| Service | Purpose | Host Port |
|--------|---------|----------:|
| blazor-frontend | Blazor UI. Communicates only with API Gateway via HttpClient. | 8080 |
| api-gateway | Single entry point. Routes `/api/*` to internal services. Provides aggregated endpoint `/api/orders/{id}/details`. | 5000 |
| product-service | Product catalog and stock. Consumes order events to update stock. | Not exposed (main mode) |
| customer-service | Customer management. | Not exposed (main mode) |
| order-service | Order creation and tracking. Validates customer/product via HTTP. Publishes events. | Not exposed (main mode) |
| notification-service | Consumes order events and stores notification logs. | Not exposed (main mode) |
| rabbitmq | Message broker + management UI. | 5672, 15672 |
| product-db | PostgreSQL database for Product Service | Internal |
| customer-db | PostgreSQL database for Customer Service | Internal |
| order-db | PostgreSQL database for Order Service | Internal |
| notification-db | PostgreSQL database for Notification Service | Internal |

## Architecture Summary

- Database-per-service: each microservice owns its database and DbContext.
- API Gateway: the gateway is the only backend component exposed to clients in main mode.
- Synchronous validation: Order Service validates customer and product existence via HTTP.
- Asynchronous messaging: events are published to RabbitMQ and consumed by Product and Notification services.
- Eventual consistency: stock updates occur asynchronously after order creation/cancellation.

## Repository Structure (High Level)

```text
.
├─ docker-compose.yml
├─ docker-compose.swagger.yml                  (optional for viva/testing)
├─ .env
└─ src
   ├─ ApiGateway/ApiGateway.Api
   ├─ CustomerService/CustomerService.Api
   ├─ ProductService/ProductService.Api
   ├─ OrderService/OrderService.Api
   ├─ NotificationService/NotificationService.Api
   ├─ Shared/Contracts
   └─ Frontend/BlazorFrontend

## Prerequisites

- Docker Desktop (Linux containers mode)
- Docker Compose (included with Docker Desktop)

## Environment Configuration

The repository uses an `.env` file for PostgreSQL password:

```env
POSTGRES_PASSWORD=postgrespw
```

RabbitMQ uses default credentials:

- Username: guest  
- Password: guest  

## Run (Main Mode: Gateway + Frontend)

From the repository root:

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

Check status:

```bash
docker compose ps
```

## URLs (Main Mode)

- Blazor Frontend: http://localhost:8080  
- API Gateway Swagger (gateway endpoints only): http://localhost:5000/swagger  
- RabbitMQ UI: http://localhost:15672 (guest/guest)  

## Optional: Viva/Testing Mode (Expose Service Swagger UIs)

If you need to open each microservice Swagger UI directly on different ports, run with the additional compose file:

```bash
docker compose -f docker-compose.yml -f docker-compose.swagger.yml up -d --build
```

### Service Swagger URLs (viva/testing mode)

- Product Service: http://localhost:5001/swagger  
- Customer Service: http://localhost:5002/swagger  
- Order Service: http://localhost:5003/swagger  
- Notification Service: http://localhost:5004/swagger  
- API Gateway: http://localhost:5000/swagger  
- Frontend: http://localhost:8080  

To return to main mode (hide internal services):

```bash
docker compose down
docker compose up -d --build
```

## Frontend Pages and Features

### Minimum required features implemented:

#### Products
- View all products  
- Add product  

#### Customers
- View customers  
- Add customer  

#### Orders
- Create order  
- View orders  

### Additional helpful features:

- Notifications page to view event logs  
- Cancel order action (publishes OrderCancelled and restores stock asynchronously)  

### Navigation:

- Home (Dashboard)  
- Products  
- Customers  
- Orders  
- Notifications  

## API Gateway Routing

The frontend communicates only with the API Gateway using the `/api` prefix:

- `/api/products` → Product Service `/products`  
- `/api/customers` → Customer Service `/customers`  
- `/api/orders` → Order Service `/orders`  
- `/api/notifications` → Notification Service `/notifications`  

### Aggregated endpoint (gateway-owned):

```http
GET /api/orders/{id}/details
```

## Messaging (RabbitMQ)

- Exchange: `orders.exchange` (direct)  

### Routing keys:
- `order.created`  
- `order.cancelled`  

Product Service consumes events to update stock.  
Notification Service consumes events to store logs.  

## End-to-End Workflow (UI Demo)

1. Open the UI: http://localhost:8080  
2. Add a customer (Customers page)  
3. Add a product with stock (Products page)  
4. Create an order (Orders page)  
5. Refresh Products page after a short delay and confirm stock decreased  
6. Open Notifications page and confirm a log entry exists  
7. Cancel the order (Orders page)  
8. Refresh Products page after a short delay and confirm stock restored  
9. Confirm cancellation log appears in Notifications  

## Database Verification (Optional)

You can inspect service databases from containers using `psql`.

Example:

```bash
docker exec -it ordermanagement-product-db-1 psql -U postgres -d productdb
```

Inside `psql`:

```sql
\dt
SELECT * FROM "Products";
SELECT * FROM "ProcessedEvents";
\q
```

Repeat similarly for:

- `customerdb` → "Customers"  
- `orderdb` → "Orders", "OrderItems"  
- `notificationdb` → "Notifications"  

## Troubleshooting

### Ports not reachable (Swagger URLs)

In main mode, internal services are not exposed. Use the viva/testing mode compose file to publish ports.

### Service not starting

Check logs:

```bash
docker compose logs --tail 200 api-gateway
docker compose logs --tail 200 blazor-frontend
docker compose logs --tail 200 product-service
docker compose logs --tail 200 customer-service
docker compose logs --tail 200 order-service
docker compose logs --tail 200 notification-service
```

### Clean rebuild

```bash
docker compose down
docker compose build --no-cache
docker compose up --build
```