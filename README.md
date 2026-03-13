# DigiMenuAPI — Solución Multi-Tenant SaaS

Plataforma SaaS para menús digitales de restaurantes. Construida en **.NET 10** con una arquitectura limpia (Clean Architecture) y separación en dos proyectos: un núcleo reutilizable y la API principal.

---

## Proyectos de la Solución

| Proyecto | Tipo | Propósito |
|---|---|---|
| **AppCore** | Class Library (.NET 10) | Núcleo reutilizable: entidades base, multi-tenancy, autenticación, email |
| **DigiMenuAPI** | ASP.NET Core Web API (.NET 10) | API REST con lógica específica de menús digitales |

`DigiMenuAPI` referencia a `AppCore` y extiende sus DbContext, servicios y entidades.

---

## Arquitectura General

```
┌─────────────────────────────────────────────────┐
│                  DigiMenuAPI                    │
│  Controllers → Services → DTOs → AppDbContext   │
│                     ↓                           │
│  ┌─────────────────────────────────────────┐    │
│  │               AppCore                   │    │
│  │  TenantService · Email · ModuleGuard    │    │
│  │  CoreDbContext · Entidades Base         │    │
│  └─────────────────────────────────────────┘    │
└─────────────────────────────────────────────────┘
```

### Capas (Clean Architecture)

```
Presentación  →  Controllers (DigiMenuAPI)
Aplicación    →  Services + DTOs + Interfaces
Dominio       →  Entities (AppCore + DigiMenuAPI)
Infraestructura → EF Core, Email, FileStorage
```

---

## Patrones Clave

| Patrón | Descripción |
|---|---|
| **Multi-Tenant (por Company)** | El JWT contiene `CompanyId` y `BranchId`. `ITenantService` resuelve el contexto en cada request |
| **OperationResult\<T\>** | Todas las respuestas de servicios usan este wrapper tipado con códigos de error y soporte i18n |
| **Soft Delete** | `IsDeleted` en entidades + `HasQueryFilter` global en EF Core |
| **Outbox de Email** | Emails guardados en `OutboxEmails` dentro de la misma transacción; un `HostedService` los procesa |
| **Feature Modules** | Funcionalidades activables por Company (Reservaciones, Pedidos Online, etc.) |
| **Audit Trail** | `CreatedAt`, `ModifiedAt`, `CreatedUserId`, `ModifiedUserId` capturados automáticamente en `SaveChangesAsync` |

---

## Jerarquía de Datos

```
Plan
 └─ Company (Tenant)
     ├─ CompanyInfo / CompanyTheme / CompanySeo  (configuración 1:1)
     ├─ CompanyModules  (funcionalidades activas)
     ├─ AppUsers
     └─ Branches
         ├─ BranchLocale / BranchSchedule / BranchSpecialDays
         ├─ AppUsers (staff)
         ├─ BranchProducts (productos activados con precio por branch)
         ├─ BranchReservationForm
         ├─ Reservations
         └─ FooterLinks

Company-level (catálogo compartido):
 Categories → Products → ProductTranslations
                       → Tags (N:N)
                       → BranchProducts (activación por branch)
```

---

## Stack Tecnológico

- **Runtime:** .NET 10
- **ORM:** Entity Framework Core 10 (SQL Server)
- **Auth:** JWT Bearer
- **Mapeo:** AutoMapper
- **Logging:** Serilog
- **Email:** SendGrid / SMTP + Outbox Pattern
- **Imágenes:** SixLabors.ImageSharp
- **Passwords:** BCrypt
- **Documentación API:** OpenAPI nativo .NET 10 + Scalar UI

---

## Documentación Detallada

- [AppCore — Núcleo reutilizable](docs/appcore.md)
- [DigiMenuAPI — API de menús](docs/digimenuapi.md)
