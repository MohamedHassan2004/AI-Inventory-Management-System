# AI-Inventory-Management-System

A clean-architecture .NET 10 Inventory Management system (API + infrastructure + tests).

This repository contains a backend solution for managing products, suppliers, stock batches, purchase orders and cashier orders. The solution is structured as a multi-project .NET 10 backend with full test coverage across all layers.

---

## Repository Layout

```
AI-Inventory-Management-System/
└── Inventory.Backend/
    ├── src/
    │   ├── Inventory.API/           — Controllers, middleware, program startup, OpenAPI
    │   ├── Inventory.Application/   — DTOs, service interfaces, validators, mapping
    │   ├── Inventory.Domain/        — Entities, enums, exceptions, domain rules
    │   └── Inventory.Infrastructure/— EF Core, repositories, queries, identity, file services
    ├── test/
    │   ├── Inventory.API.Tests/
    │   ├── Inventory.Application.Tests/
    │   ├── Inventory.Domain.Tests/
    │   └── Inventory.Infrastructure.Tests/
    └── Inventory.Backend.slnx
```

---

## Architecture Overview

This solution follows **Clean Architecture** with four main layers:

- **Domain** — core business entities, enums, exceptions and domain logic.
- **Application** — DTOs, service interfaces and application services (use-cases), validation and mapping.
- **Infrastructure** — EF Core DbContext, repositories, queries, external services (file, auth), and settings.
- **API** — ASP.NET Core controllers, request/response handling, middleware and program startup.

```
+-----------+      depends on      +--------------+
|  API/UI   | ------------------>  | Application  |
+-----------+                      +--------------+
     |                                     |
     v                                     v
+--------------+      depends on      +--------------+
|Infrastructure| -------------------> |   Domain     |
+--------------+                      +--------------+
```

---

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10.0 |
| Web Framework | ASP.NET Core Web API |
| ORM | Entity Framework Core (SQL Server) |
| Auth | ASP.NET Core Identity + JWT |
| Mapping | Mapster |
| Validation | FluentValidation |
| Logging | Serilog |
| API Docs | Scalar.AspNetCore + OpenAPI |

---

## Quick Start

### Prerequisites

- .NET 10 SDK
- SQL Server (or compatible) accessible from the app
- (Optional) `dotnet-ef` global tool for migrations:
  ```bash
  dotnet tool install --global dotnet-ef
  ```

### 1. Clone & Restore

```bash
git clone <repo-url>
cd AI-Inventory-Management-System
dotnet restore Inventory.Backend/Inventory.Backend.slnx
dotnet build Inventory.Backend/Inventory.Backend.slnx
```

### 2. Configure `appsettings.json`

Edit `Inventory.Backend/src/Inventory.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=InventoryDb;Trusted_Connection=True;"
  },
  "JWT": {
    "Secret": "your-secret-key",
    "AccessTokenDurationInMinutes": 60,
    "RefreshTokenDurationInDays": 7
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000"]
  }
}
```

### 3. Apply EF Core Migrations

From the repository root:

```bash
dotnet ef database update \
  --project Inventory.Backend/src/Inventory.Infrastructure/Inventory.Infrastructure.csproj \
  --startup-project Inventory.Backend/src/Inventory.API/Inventory.API.csproj
```

> **Note:** The app seeds roles and an admin account on first startup — ensure migrations are applied before running for the first time.

### 4. Run the API

```bash
dotnet run --project Inventory.Backend/src/Inventory.API/Inventory.API.csproj
```

When running in **Development**, the Scalar API reference UI and OpenAPI spec are available automatically.

### 5. Run Tests

```bash
dotnet test Inventory.Backend/Inventory.Backend.slnx
```

---

## API Endpoints

Base route: `api/<controller>`

### Products

| Method | Route | Description |
|---|---|---|
| POST | `/api/products` | Create a product |
| PUT | `/api/products/{id}` | Update a product |
| PATCH | `/api/products/{id}/updatePrice` | Update product price |
| PATCH | `/api/products/{id}/updateReorderPoint` | Update reorder point |
| GET | `/api/products` | Get all products |
| GET | `/api/products/low-stock` | Products at or below reorder point |
| GET | `/api/products/{id}` | Get product by ID |
| DELETE | `/api/products/{id}` | Delete product |
| GET | `/api/products/search?q=` | Search by name or SKU (Cashier / Admin) |

### Suppliers

| Method | Route | Description |
|---|---|---|
| GET | `/api/suppliers` | List suppliers |
| GET | `/api/suppliers/{id}` | Get supplier details |
| POST | `/api/suppliers` | Create supplier |
| PUT | `/api/suppliers/{id}` | Update supplier |
| DELETE | `/api/suppliers/{id}` | Soft-delete supplier |
| PUT | `/api/suppliers/{id}/restore` | Restore soft-deleted supplier |
| GET | `/api/suppliers/{id}/notes` | Get supplier notes |
| POST | `/api/suppliers/{id}/ratings` | Add supplier rating |

### Purchase Orders *(Admin / InventoryStaff)*

| Method | Route | Description |
|---|---|---|
| POST | `/api/purchaseorders/submit` | Submit a purchase order with items |
| GET | `/api/purchaseorders/{id}` | Get purchase order by ID |
| GET | `/api/purchase-orders/{id}/invoice` | Generate PDF invoice for purchase order |
| GET | `/api/purchaseorders` | List all purchase orders (with filtering) |
| GET | `/api/purchaseorders/{id}/items` | Get items for a purchase order |

### Stock Batches

| Method | Route | Description |
|---|---|---|
| GET | `/api/stockbatches` | List all stock batches |
| GET | `/api/stockbatches/{id}` | Get batch by ID |
| GET | `/api/stockbatches/product/{productId}` | Get batches for a product |
| GET | `/api/stockbatches/expiring/{days}` | Batches expiring within N days |
| POST | `/api/stockbatches` | Create a stock batch |
| PUT | `/api/stockbatches/{id}` | Update a batch |
| DELETE | `/api/stockbatches/{id}` | Delete a batch |

### Orders *(Cashier / Admin)*

| Method | Route | Description |
|---|---|---|
| POST | `/api/orders/submit` | Submit an order (single-shot) |
| POST | `/api/orders/draft` | Create a draft order |
| POST | `/api/orders/{id}/items` | Add item to draft order |
| DELETE | `/api/orders/{id}/items/{productId}` | Remove item from draft order |
| POST | `/api/orders/{id}/confirm` | Confirm draft (consumes stock FEFO) |
| DELETE | `/api/orders/{id}` | Cancel a draft order |
| GET | `/api/orders/{id}` | Get order by ID |
| GET | `/api/orders/{id}/receipt` | Generate PDF receipt for order |
| GET | `/api/orders` | List orders (status, sorting, paging) |
| GET | `/api/orders/{orderId}/items` | Get order items |

### Return Orders *(Cashier / Admin)*

| Method | Route | Description |
|---|---|---|
| POST | `/api/return-orders` | Create a return order |
| GET | `/api/return-orders/{id}` | Get return order by ID |
| GET | `/api/return-orders` | List return orders (filter + paging) |

### Dashboard & Reports

| Method | Route | Description |
|---|---|---|
| GET | `/api/dashboard/summary` | Dashboard summary metrics |
| GET | `/api/reports/inventory/expiring-batches` | Batches expiring soon |
| GET | `/api/reports/inventory/dead-stock` | Dead stock analysis |
| GET | `/api/reports/inventory/low-stock` | Low stock products |
| GET | `/api/reports/inventory/out-of-stock` | Out of stock products |
| GET | `/api/reports/inventory/turnover` | Inventory turnover rate |
| GET | `/api/reports/returns/top-products` | Top returned products |
| GET | `/api/reports/sales/top-products` | Top selling products |
| GET | `/api/reports/sales/analytics` | Sales analytics data |
| GET | `/api/reports/sales/profit-margin` | Profit margin analysis |
| GET | `/api/reports/users/cashier-sales` | Cashier sales performance |
| GET | `/api/reports/users/status-breakdown` | User status breakdown |

### Categories

| Method | Route | Description |
|---|---|---|
| POST | `/api/categories` | Create category (supports image upload) |
| PUT | `/api/categories/{id}` | Update category |
| PATCH | `/api/categories/updateCategoryImg/{id}` | Update category image |
| GET | `/api/categories` | List categories |
| DELETE | `/api/categories/{id}` | Delete category |

### Auth & Users

| Method | Route | Description |
|---|---|---|
| POST | `/api/auth/login` | Login — returns JWT + refresh token cookie |
| POST | `/api/auth/register` | Register user (Admin only) |
| POST | `/api/auth/logout` | Logout — invalidates refresh token |
| POST | `/api/auth/refresh-token` | Refresh JWT via cookie |
| PUT | `/api/auth/change-password` | Change current user's password |
| GET | `/api/auth/is-username-exist` | Check username availability |
| GET | `/api/auth/is-email-exist` | Check email availability |
| GET | `/api/auth/is-phone-number-exist` | Check phone number availability |

### User Profile & Admin

| Method | Route | Description |
|---|---|---|
| POST | `/api/users/identity-image` | Upload identity image (UploadIdentity policy) |
| GET | `/api/users/status` | Get current user's account status |
| GET | `/api/users/rejection-reason` | Get identity rejection reason |
| GET | `/api/admin/users` | Admin: list all accounts |
| GET | `/api/admin/users/pending` | Admin: list pending accounts |
| DELETE | `/api/admin/users/{userId}` | Admin: delete user |
| PUT | `/api/admin/users/{userId}/restore` | Admin: restore user |
| PUT | `/api/admin/users/{userId}/approve` | Admin: approve account |
| PUT | `/api/admin/users/{userId}/reject?reason=` | Admin: reject account with reason |

### Roles *(Admin only)*

| Method | Route | Description |
|---|---|---|
| GET | `/api/roles/{userId}` | Get user roles |
| POST | `/api/roles/{userId}?role=` | Add role to user |
| DELETE | `/api/roles/{userId}?role=` | Remove role from user |

---

## Key Features

- **Clean Architecture** separation across Domain, Application, Infrastructure, and API layers.
- **JWT authentication** with refresh tokens via a secure cookie-based refresh flow.
- **Role- and policy-based authorization** — Admin, InventoryStaff, Cashier, and Active account policies.
- **Full inventory lifecycle** — products, stock batches with FEFO consumption, and purchase orders.
- **Draft order workflow** — cashiers build orders incrementally with expiration, confirm, and cancel support.
- **Return orders** — create returns against completed orders, track refunded quantities, and restock with new expiry dates.
- **Dashboard & Reports** — extensive reporting on sales, returns, purchases, inventory status, and user performance.
- **Soft-delete** for Suppliers and Categories with restore capability.
- **File upload** support for category images and user identity images.
- **Serilog** structured logging and SQL Server health checks.
- **Comprehensive test coverage** across all four layers (unit + integration).

---

## Database Schema

| Entity | Key Relationships |
|---|---|
| `Product` | Has many `StockBatch`; optional `CategoryId` |
| `StockBatch` | Belongs to `Product` and `Supplier`; tracks `OriginalQuantity` / `RemainingQuantity` |
| `ReturnOrder` | Belongs to `Order`; includes `TotalRefundAmount`, `CashierId`, and `ReturnDate` |
| `ReturnOrderItem` | Belongs to `ReturnOrder` and `OrderItem`; captures refunded quantity and new expiry date |
| `Supplier` | Has many `SupplierNotes`; soft-deleted via `IsDeleted` query filter |
| `Order` | Has many `OrderItem`; tracks draft/confirm flow, financials, and `CashierId` |
| `PurchaseOrder` | Has many `PurchaseOrderItem`; on submit, items create stock batches and status → Completed |
| `Category` | Has many `Product`; soft-deleted; supports image upload |

See `Inventory.Backend/src/Inventory.Infrastructure/Migrations/` for the full schema history.

---

## Design Decisions

- **Clean Architecture** — domain logic is fully independent of frameworks and infrastructure, making it testable in isolation.
- **Repository + Unit of Work** — `IRepository<T>` abstracts data access; `IUnitOfWork` scopes transactions consistently.
- **Domain-Driven Behavior** — entities like `Product`, `Order`, and `PurchaseOrder` encapsulate business rules (FEFO stock consumption, reorder checks, status transitions) rather than delegating to services.
- **CQRS-style separation** — read-model queries live under `Queries/`; write operations go through application services.
- **Mapster** — lightweight, high-performance mapping with no reflection overhead at runtime.
- **FluentValidation** — keeps request validation out of controllers and close to the use-case boundary.

---

## Contributing

Open issues or pull requests for bug fixes and features. Before submitting a PR, ensure the solution builds and all tests pass:

```bash
dotnet build Inventory.Backend/Inventory.Backend.slnx
dotnet test Inventory.Backend/Inventory.Backend.slnx
```