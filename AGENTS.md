# Copilot Agent Instructions

This workspace is a .NET 10.0 project implementing Clean Architecture for an AI Inventory Management System.

## 🏗️ Architecture & Core Principles
- **Clean Architecture Hierarchy** (Respect the dependency flow):
  - **Domain**: `Inventory.Domain` (Core business logic, Entities, Enums, Exceptions). No external dependencies.
  - **Application**: `Inventory.Application` (Services, DTOs, Validation, Mappings). Depends on Domain.
  - **Infrastructure**: `Inventory.Infrastructure` (Data access, Entity Framework Core, Repositories, Migrations). Depends on Domain & Application.
  - **API**: `Inventory.API` (Controllers, Middlewares, Extensions). Entry point, depends on Application & Infrastructure.
- **Dependency Injection**: Use `DependencyInjection.cs` inside respective layers (Application, Infrastructure) to register services.

## 🛠️ Tech Stack & Libraries
- **Framework**: .NET 10.0 (ASP.NET Core Web API)
- **Data Access**: Entity Framework Core
- **Identity & Auth**: ASP.NET Core Identity
- **Mapping**: Mapster (`MapsterMapper`)
- **Logging**: Serilog
- **API Documentation**: OpenAPI / Scalar (`Scalar.AspNetCore`)

## 💻 Common Commands
- **Build**: `dotnet build`
- **Run API**: `dotnet run --project src/Inventory.API/Inventory.API.csproj`
- **Add Migration**: `dotnet ef migrations add <Name> --project src/Inventory.Infrastructure/Inventory.Infrastructure.csproj --startup-project src/Inventory.API/Inventory.API.csproj`
- **Database Update**: `dotnet ef database update --project src/Inventory.Infrastructure/Inventory.Infrastructure.csproj --startup-project src/Inventory.API/Inventory.API.csproj`

## 📖 Coding Guidelines
- **DTOs & Mapping**: DTOs should reside in `Inventory.Application/DTOs`. Use Mapster for mapping between Entities and DTOs.
- **Error Handling**: Throw domain specific exceptions and handle them globally using `GlobalExceptionHandlingMiddleware.cs`.
- **Validation**: Place validation logic or filters under `Inventory.Application/Validation` or `Filter` folders.
- **Controllers**: Keep controllers lean. Inject Application interfaces/services to handle business logic.
- **Localization**: This project uses custom Localization. Ensure messages and user-facing strings utilize it appropriately (e.g., via `ILocalizationService`).

## 🤖 Agent Responsibilities
- **Update README.md**: After any significant addition or change to the project, ensure the `README.md` file is updated to reflect these changes.

## 🔗 Related Resources
- *No internal Markdown documentation found. Please maintain architectural boundaries when creating new features.*
