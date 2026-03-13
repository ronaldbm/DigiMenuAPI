# AppCore — Documentación

Librería de clase (.NET 10) que contiene la lógica reutilizable de la plataforma. Cualquier proyecto nuevo puede referenciarla para obtener multi-tenancy, autenticación, email y gestión de módulos sin reescribirlos.

---

## Estructura de Carpetas

```
AppCore/
├── Domain/
│   └── Entities/              # Entidades del dominio (DDD)
├── Application/
│   ├── Interfaces/            # Contratos de servicios del núcleo
│   ├── Services/              # Implementaciones (Tenant, Email, ModuleGuard)
│   ├── Common/                # Utilitarios compartidos
│   └── Utils/                 # Helpers de inicialización y soporte
└── Infrastructure/
    ├── SQL/                   # CoreDbContext (EF Core)
    ├── Email/                 # Processor, Renderer, Templates HTML
    └── Entities/              # Outbox y reset de contraseña
```

---

## Domain/Entities

Entidades del dominio siguiendo principios DDD. Todas heredan de `BaseEntity`.

### BaseEntity
Clase abstracta base de todas las entidades.

| Campo | Tipo | Descripción |
|---|---|---|
| `CreatedAt` | DateTime | Fecha de creación (UTC) |
| `ModifiedAt` | DateTime? | Fecha de última modificación |
| `CreatedUserId` | string? | Id del usuario que creó el registro |
| `ModifiedUserId` | string? | Id del usuario que lo modificó |

Estos campos se llenan automáticamente en `CoreDbContext.SaveChangesAsync()` con los claims del JWT activo.

---

### Plan
Planes de suscripción de la plataforma.

| Campo | Descripción |
|---|---|
| `Name` | Nombre del plan (Basic, Pro, Business, Enterprise) |
| `MonthlyPrice` / `AnnualPrice` | Precios |
| `MaxBranches` | Límite de sucursales permitidas |
| `MaxUsers` | Límite de usuarios permitidos |
| `IsActive` | Si el plan está disponible para contratar |

---

### Company *(Tenant raíz)*
Representa una empresa/cadena de restaurantes. Es la raíz del tenant en el sistema multi-tenant.

| Campo | Descripción |
|---|---|
| `Slug` | Identificador único en URL (ej: `mi-restaurante`). Índice único. |
| `Email` | Email del propietario. Índice único. |
| `PlanId` | Plan de suscripción activo |
| `MaxBranches` / `MaxUsers` | Límites copiados del plan al registrarse |
| `IsActive` | Si la company está activa (puede ser suspendida) |

**Relaciones:** `Branches`, `AppUsers`, `CompanyInfo`, `CompanyTheme`, `CompanySeo`, `CompanyModules`

---

### Branch *(Sucursal)*
Ubicación física de una Company.

| Campo | Descripción |
|---|---|
| `CompanyId` | FK a Company (tenant) |
| `Name` | Nombre de la sucursal |
| `IsDeleted` | Soft delete. Filtro global en EF Core: siempre excluye registros borrados. |
| `MaxReservationCapacity` | Capacidad máxima de personas por reserva |

**Relaciones:** `BranchLocale`, `BranchSchedules`, `BranchSpecialDays`, `AppUsers`, `Reservations`

> El soft delete se implementa con `HasQueryFilter(b => !b.IsDeleted)`. Para ver registros borrados usar `.IgnoreQueryFilters()`.

---

### AppUser *(Usuario)*
Usuarios del sistema con roles jerárquicos.

| Rol | Valor | Descripción |
|---|---|---|
| `SuperAdmin` | 255 | Administrador de la plataforma |
| `CompanyAdmin` | 1 | Administrador de una Company |
| `BranchAdmin` | 2 | Administrador de una Branch |
| `Staff` | 3 | Personal de una Branch |

| Campo | Descripción |
|---|---|
| `CompanyId` | FK a Company |
| `BranchId` | FK a Branch (null para CompanyAdmin) |
| `Email` | Email de login. Índice único. |
| `PasswordHash` | Hash BCrypt |
| `Role` | Valor numérico del rol |

---

### CompanyInfo / CompanyTheme / CompanySeo
Entidades de configuración 1:1 con Company.

**CompanyInfo** — Información de marca:
`BusinessName`, `Tagline`, `LogoUrl`, `FaviconUrl`, `Address`, `Phone`, `SocialLinks`

**CompanyTheme** — Estilos visuales:
`PrimaryColor`, `SecondaryColor`, `AccentColor`, `BackgroundColor`, `TextColor`, `HeaderStyle`, `LayoutType`, `FontFamily`

> Los colores por defecto están en `AppCore/Application/Common/DefaultTheme.cs`

**CompanySeo** — Metadatos SEO:
`MetaTitle`, `MetaDescription`, `Keywords`, `GoogleAnalyticsId`, `FacebookPixelId`

---

### BranchLocale
Configuración regional 1:1 con Branch.

| Campo | Ejemplo |
|---|---|
| `CurrencyCode` | `"USD"`, `"CRC"` |
| `CurrencySymbol` | `"$"`, `"₡"` |
| `LanguageCode` | `"es"`, `"en"` |
| `TimeZone` | `"America/Costa_Rica"` |
| `PhoneCode` | `"+506"` |

Inicializada automáticamente al crear una Branch por `BranchLocaleInitializer`.

---

### BranchSchedule / BranchSpecialDay
Horarios de apertura por Branch.

**BranchSchedule** — Horario semanal:
- Un registro por día de la semana (`DayOfWeek`: Monday–Sunday)
- `OpenTime`, `CloseTime`, `IsClosed`
- Inicializado con `BranchScheduleInitializer` al crear la Branch

**BranchSpecialDay** — Días especiales:
- `Date`, `Reason`, `IsClosed`, `OpenTime`, `CloseTime`
- Para feriados, días con horario especial, etc.

---

### Modules (PlatformModule / CompanyModule)
Sistema de feature flags por tenant.

**PlatformModule** — Catálogo de funcionalidades disponibles:

| Código | Funcionalidad |
|---|---|
| `RESERVATIONS` | Sistema de reservas de mesas |
| `TABLE_MANAGEMENT` | Gestión de mesas |
| `ANALYTICS` | Analíticas y reportes |
| `ONLINE_ORDERS` | Pedidos online |

**CompanyModule** — Activación por Company:
- `CompanyId`, `PlatformModuleId`, `IsActive`, `ExpiresAt`
- El `IModuleGuard` verifica en runtime si un módulo está activo para el tenant actual

---

### StandardIcon
Íconos SVG de redes sociales a nivel plataforma.
`Name` (Facebook, Instagram, WhatsApp, TikTok, YouTube, X, LinkedIn), `SvgContent`

---

## Application/Interfaces

### ITenantService
Resuelve el contexto del tenant desde los claims del JWT.

```csharp
string GetCompanyId()
string? GetBranchId()
string GetUserId()
Task<Company?> ResolveBySlugAsync(string slug)
Task<bool> ValidateBranchOwnershipAsync(string branchId)
```

`ValidateBranchOwnershipAsync` garantiza que el usuario solo pueda acceder a Branches de su propia Company (aislamiento multi-tenant).

---

### IEmailService / IEmailQueueService
- `IEmailService`: Envío directo (SendGrid o SMTP)
- `IEmailQueueService`: Encola el email en `OutboxEmails` dentro de la transacción actual

---

### IModuleGuard

```csharp
Task<bool> IsActiveAsync(string companyId, string moduleCode)
```

Verifica si una funcionalidad está activa para la Company. Usado por los servicios para retornar `OperationResult.ModuleRequired()` si no está activo.

---

## Application/Services

| Servicio | Qué hace |
|---|---|
| `TenantService` | Implementa `ITenantService`. Extrae `CompanyId`/`BranchId`/`UserId` del `HttpContext.User`. |
| `ModuleGuard` | Implementa `IModuleGuard`. Consulta `CompanyModules` con caché en memoria. |
| `SendGridEmailService` | Envío via SendGrid API |
| `SmtpEmailService` | Envío via SMTP (fallback) |
| `EmailQueueService` | Crea registros `OutboxEmail` en la BD |

---

## Application/Common

### OperationResult\<T\>
Wrapper estándar para todas las respuestas de servicios.

```csharp
// Casos de uso:
OperationResult<ProductDto>.Ok(dto)
OperationResult<ProductDto>.NotFound("PRODUCT_NOT_FOUND")
OperationResult<ProductDto>.Forbidden()
OperationResult<ProductDto>.Conflict("SLUG_TAKEN")
OperationResult<ProductDto>.ValidationError("INVALID_EMAIL")
OperationResult<ProductDto>.ModuleRequired("RESERVATIONS")
```

El `BaseController.HandleResult<T>()` en DigiMenuAPI convierte esto automáticamente al código HTTP correcto (200, 404, 403, 409, 422).

---

### ErrorKeys (Enum)
Claves de localización para mensajes de error en el frontend:
`NotFound`, `Forbidden`, `EmailAlreadyExists`, `InvalidCredentials`, `SlugTaken`, `ModuleRequired`, `InvalidToken`, etc.

### ErrorCodes
Mapeo a códigos HTTP:
- `NotFound` → 404
- `Forbidden` / `ModuleRequired` → 403
- `Conflict` → 409
- `Validation` → 422
- `Unauthorized` → 401

### UserRoles
Constantes de roles y helpers:
```csharp
UserRoles.CompanyAdmin   // "1"
UserRoles.BranchAdmin    // "2"
UserRoles.IsAdmin(role)  // true si es CompanyAdmin o superior
```

### PagedResult\<T\>
Wrapper para resultados paginados: `Items`, `TotalCount`, `Page`, `PageSize`.

### ReservationStatus (Enum)
`Pending`, `Confirmed`, `Cancelled`, `Completed`, `NoShow`

---

## Application/Utils

| Utilidad | Qué hace |
|---|---|
| `BranchScheduleInitializer` | Crea los 7 registros de `BranchSchedule` al crear una Branch |
| `BranchLocaleInitializer` | Asigna locale por defecto según el país de la Company |
| `SlugHelper` | Genera slugs URL-safe y valida unicidad |
| `PasswordValidator` | Reglas de complejidad de contraseña |
| `LocaleHelper` | Helpers de formato de fecha/moneda |
| `LogMessageDispatcher` | Wrapper de Serilog con contexto de tenant |

---

## Infrastructure/SQL — CoreDbContext

DbContext abstracto que gestiona todas las entidades del núcleo.

**Características clave:**

1. **Audit automático:** En `SaveChangesAsync`, recorre los `ChangeTracker` entries y asigna `CreatedAt`/`ModifiedAt`/`CreatedUserId`/`ModifiedUserId` usando los claims del JWT.

2. **Query filters globales:** `Branch`, `AppUser` y entidades con soft-delete tienen `HasQueryFilter(e => !e.IsDeleted)`.

3. **Seeding inicial:**
   - 4 Planes (Basic, Pro, Business, Enterprise)
   - 4 PlatformModules (RESERVATIONS, TABLE_MANAGEMENT, ANALYTICS, ONLINE_ORDERS)
   - 7 StandardIcons (redes sociales)
   - Company y Branch de demo

4. **Cascade/Restrict:** Configurado para cada relación con Fluent API. Ej: `Branch → BranchLocale` es Cascade; `AppUser → Company` es Restrict para evitar borrado accidental.

---

## Infrastructure/Email

### Outbox Pattern
1. El servicio llama a `IEmailQueueService.EnqueueAsync(...)` dentro de la misma transacción de negocio.
2. Se crea un `OutboxEmail` con estado `Pending`.
3. `EmailOutboxProcessor` (IHostedService) corre cada N segundos, consulta emails Pending y los envía.
4. Si el envío falla, actualiza estado a `Failed` y reintenta con backoff exponencial.
5. Al éxito, estado cambia a `Sent`.

### Templates disponibles
| Template | Cuándo se usa |
|---|---|
| `welcome.html` | Registro de nueva Company |
| `forgot-password.html` | Solicitud de reset de contraseña |
| `temporary-password.html` | Creación de usuario con contraseña temporal |
| `reservation-confirmation.html` | Confirmación de reserva al cliente |

`EmailTemplateRenderer` reemplaza variables `{{NombreVariable}}` dentro del HTML.

---

## Infrastructure/Entities — Outbox y Reset

### OutboxEmail / OutboxEmailBody
Implementan el patrón Outbox para entrega garantizada de emails:
- `OutboxEmail`: `To`, `Subject`, `Status (Pending/Sent/Failed)`, `RetryCount`, `SentAt`
- `OutboxEmailBody`: HTML del email (separado para no cargar el body en consultas de monitoreo)

### PasswordResetRequest
Tokens para reset de contraseña:
- `UserId`, `Token` (GUID seguro), `ExpiresAt`, `IsUsed`
- Un token se invalida al usarse o al expirar (tiempo configurable)
