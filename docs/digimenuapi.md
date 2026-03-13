# DigiMenuAPI — Documentación

API REST (.NET 10) que extiende AppCore con la lógica específica de la plataforma de menús digitales para restaurantes.

---

## Estructura de Carpetas

```
DigiMenuAPI/
├── Controllers/               # Endpoints REST (13 controladores)
├── Application/
│   ├── Interfaces/            # Contratos de servicios (15)
│   ├── Services/              # Implementaciones de servicios (14)
│   ├── Common/                # AutoMapperProfiles
│   └── DTOs/
│       ├── Read/              # DTOs de lectura (respuestas)
│       ├── Create/            # DTOs de creación
│       └── Update/            # DTOs de actualización
├── Infrastructure/
│   ├── SQL/                   # ApplicationDbContext
│   └── Entities/              # Entidades específicas de DigiMenu
├── Migrations/                # Migraciones EF Core
└── Program.cs                 # Configuración DI y middleware
```

---

## Program.cs — Configuración del Host

### Dependency Injection registrada

```
HttpContextAccessor
ApplicationDbContext (extiende CoreDbContext)
IAuthService → AuthService
ISettingService → SettingService
IProductService → ProductService
ICategoryService → CategoryService
ITagService → TagService
IBranchService → BranchService
IUserService → UserService
IReservationService → ReservationService
IScheduleService → ScheduleService
IStoreService → StoreService
IFooterLinkService → FooterLinkService
IStandardIconService → StandardIconService
ICacheService → CacheService
IModuleService → ModuleService
IEmailQueueService → EmailQueueService
IEmailService → SendGridEmailService | SmtpEmailService
ITenantService → TenantService
IModuleGuard → ModuleGuard
AutoMapper (todos los perfiles)
MemoryCache
EmailOutboxProcessor (HostedService)
```

### Cadena de Middleware

```
1. ExceptionHandler (maneja errores 401 especiales de JWT)
2. Serilog request logging
3. HTTPS Redirect
4. CORS (orígenes configurables por appsettings)
5. JWT Bearer Authentication
6. Authorization
7. Rate Limiting  → /auth: 10 req/min
8. Output Caching → rutas públicas de menú
9. Static Files
10. MapControllers
```

---

## Controllers

Todos heredan de `BaseController` que provee `HandleResult<T>(OperationResult<T>)` — convierte el resultado del servicio al código HTTP correcto.

| Código HTTP | Caso |
|---|---|
| 200 / 201 | Ok / Created |
| 400 | ValidationError |
| 401 | Unauthorized (JWT) |
| 403 | Forbidden / ModuleRequired |
| 404 | NotFound |
| 409 | Conflict |

---

### AuthController — `/api/auth`

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| POST | `/register` | No | Registra nueva Company + CompanyAdmin |
| POST | `/login` | No | Login, retorna JWT |
| PUT | `/change-password` | Sí | Cambia contraseña del usuario actual |
| POST | `/forgot-password` | No | Envía email con token de reset |
| GET | `/validate-token/{token}` | No | Valida si el token de reset es válido |
| POST | `/reset-password` | No | Aplica nueva contraseña con el token |

**Flujo de registro:**
1. Valida que email/slug no existan
2. Crea Company con `PlanId` básico
3. Crea CompanyAdmin
4. Inicializa CompanyInfo, CompanyTheme, CompanySeo con defaults
5. Encola email de bienvenida
6. Retorna JWT

---

### ProductController — `/api/products`

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/` | Lista productos de la Company |
| GET | `/{id}` | Detalle de un producto |
| POST | `/` | Crea producto (nivel Company) |
| PUT | `/{id}` | Actualiza producto |
| DELETE | `/{id}` | Soft delete |
| GET | `/branch/{branchId}` | Productos activados en una Branch |
| POST | `/branch/{branchId}` | Activa producto en Branch con precio |
| PUT | `/branch/{branchId}/{id}` | Actualiza activación (precio/imagen override) |

**Concepto clave — Product vs BranchProduct:**
- `Product` es el catálogo global de la Company (nombre, imagen base, categoría)
- `BranchProduct` activa ese producto en una Branch específica, permitiendo sobreescribir el precio y la imagen

---

### CategoryController — `/api/categories`

CRUD de categorías del menú (Entradas, Platos Fuertes, Postres, etc.)

| Campo relevante | Descripción |
|---|---|
| `CompanyId` | Categorías son a nivel Company (compartidas entre branches) |
| `DisplayOrder` | Orden de aparición en el menú |
| `Translations` | Traducciones por `LanguageCode` |
| `IsActive` | Si la categoría se muestra en el menú público |

---

### TagsController — `/api/tags`

CRUD de etiquetas de productos (Vegano, Picante, Sin Gluten, Popular, etc.)

| Campo | Descripción |
|---|---|
| `Color` | Color HEX para el badge visual |
| `Translations` | Nombre traducido por idioma |

Los Tags tienen relación N:N con Products via `ProductTags`.

---

### SettingsController — `/api/settings`

Gestión de configuración visual y regional.

| Método | Ruta | Entidad | Descripción |
|---|---|---|---|
| GET/PUT | `/company/info` | CompanyInfo | Nombre, logo, favicon, contacto |
| GET/PUT | `/company/theme` | CompanyTheme | Colores, fuente, layout |
| GET/PUT | `/company/seo` | CompanySeo | Títulos SEO, analytics |
| GET/PUT | `/branch/{id}/locale` | BranchLocale | Moneda, idioma, zona horaria |
| GET/PUT | `/branch/{id}/reservation-form` | BranchReservationForm | Config del formulario de reservas |

---

### ReservationsController — `/api/reservations`

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/` | Lista reservas de la Branch (paginado) |
| GET | `/{id}` | Detalle de reserva |
| POST | `/` | Crea reserva (pública o interna) |
| PUT | `/{id}/status` | Actualiza estado |

**Estados** (`ReservationStatus`): `Pending` → `Confirmed` / `Cancelled` → `Completed` / `NoShow`

Al confirmar, se encola email de confirmación al cliente.

> Requiere módulo `RESERVATIONS` activo para la Company.

---

### ScheduleController — `/api/schedule`

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/branch/{id}` | Horario semanal de la Branch |
| PUT | `/branch/{id}` | Actualiza horario semanal |
| GET | `/branch/{id}/special-days` | Días especiales |
| POST | `/branch/{id}/special-days` | Agrega día especial |
| DELETE | `/branch/{id}/special-days/{dayId}` | Elimina día especial |

---

### BranchesController — `/api/branches`

CRUD de sucursales de la Company. Al crear una Branch:
1. Inicializa `BranchLocale` con `BranchLocaleInitializer`
2. Inicializa `BranchSchedule` (7 días) con `BranchScheduleInitializer`
3. Inicializa `BranchReservationForm` con configuración por defecto

---

### UsersController — `/api/users`

CRUD de usuarios. Reglas de negocio:
- CompanyAdmin puede crear BranchAdmin y Staff
- BranchAdmin solo puede crear Staff en su Branch
- Al crear usuario se envía email con contraseña temporal

---

### MenuController — `/api/menu/{slug}`

Endpoint **público** (sin autenticación). Retorna el menú completo de una Branch.

Compuesto por `StoreService`:
- Datos de la Company (info, tema, SEO)
- Datos de la Branch (locale, horario, links)
- Categorías con sus productos activados en esa Branch
- Traducciones en el idioma solicitado

Usa **Output Cache** para performance en lecturas frecuentes.

---

### FooterLinksController — `/api/footer-links`

CRUD de enlaces del footer/redes sociales por Branch.

| Campo | Descripción |
|---|---|
| `Label` | Nombre del enlace |
| `Url` | URL destino |
| `StandardIconId` | FK a StandardIcon (ícono de la plataforma) |
| `CustomSvgContent` | SVG personalizado (alternativa a StandardIcon) |

---

### StandardIconsController — `/api/standard-icons`

GET de solo lectura. Retorna el catálogo de íconos SVG disponibles de la plataforma.

---

## Infrastructure/Entities

Entidades específicas de DigiMenu (no están en AppCore).

### Category
Categoría del menú a nivel Company.

| Campo | Descripción |
|---|---|
| `CompanyId` | FK a Company |
| `Name` | Nombre base (idioma por defecto) |
| `DisplayOrder` | Posición en el menú |
| `IsActive` | Visible en menú público |
| `IsDeleted` | Soft delete |
| `Translations` | Lista de `CategoryTranslation` |

### Product
Ítem del menú a nivel Company.

| Campo | Descripción |
|---|---|
| `CompanyId` | FK a Company |
| `CategoryId` | FK a Category |
| `Name` | Nombre base |
| `Description` | Descripción base |
| `MainImageUrl` | Imagen principal del producto |
| `BasePrice` | Precio base referencial |
| `IsActive` / `IsDeleted` | Control de visibilidad |
| `Tags` | Colección `Tag` (N:N via ProductTags) |
| `BranchProducts` | Activaciones en branches |
| `Translations` | Lista de `ProductTranslation` |

### BranchProduct
Activa un `Product` en una `Branch` específica con configuración local.

| Campo | Descripción |
|---|---|
| `BranchId` | FK a Branch |
| `ProductId` | FK a Product |
| `Price` | Precio en esa Branch (sobreescribe BasePrice) |
| `ImageUrl` | Imagen override (opcional) |
| `IsActive` | Si se muestra en el menú de esa Branch |
| `DisplayOrder` | Orden en la categoría |

### Tag
Etiqueta de producto a nivel Company.

| Campo | Descripción |
|---|---|
| `Name` | Nombre base |
| `Color` | Color HEX del badge |
| `Translations` | Lista de `TagTranslation` |

### Reservation
Reserva de mesa de un cliente.

| Campo | Descripción |
|---|---|
| `BranchId` | FK a Branch |
| `CustomerName` | Nombre del cliente |
| `CustomerEmail` | Email para confirmación |
| `CustomerPhone` | Teléfono |
| `ReservationDate` | Fecha y hora de la reserva |
| `PartySize` | Número de personas |
| `TableNumber` | Mesa solicitada (opcional) |
| `Status` | `Pending/Confirmed/Cancelled/Completed/NoShow` |
| `Notes` | Notas del cliente |
| `IsDeleted` | Soft delete |

### FooterLink
Enlace de redes sociales/footer por Branch.

### BranchReservationForm
Configura qué campos se muestran y cuáles son obligatorios en el formulario de reservas público:
`ShowPhone`, `RequirePhone`, `ShowTable`, `RequireTable`, `ShowPersons`, `RequirePersons`, etc.

### Translations (CategoryTranslation / ProductTranslation / TagTranslation)
Patrón de traducción: cada entidad traducible tiene una colección de registros con `LanguageCode` y el campo traducido (`Name`, `Description`).

---

## Infrastructure/SQL — ApplicationDbContext

Extiende `CoreDbContext` (AppCore) con las entidades de DigiMenu.

**DbSets adicionales:**
```csharp
DbSet<Category> Categories
DbSet<Product> Products
DbSet<Tag> Tags
DbSet<BranchProduct> BranchProducts
DbSet<Reservation> Reservations
DbSet<FooterLink> FooterLinks
DbSet<BranchReservationForm> BranchReservationForms
DbSet<CategoryTranslation> CategoryTranslations
DbSet<ProductTranslation> ProductTranslations
DbSet<TagTranslation> TagTranslations
```

**Seeding de demo:**
- 6 Tags (Vegano, Picante, Alcohólico, Sin Gluten, Popular, Nuevo)
- 5 Categories (Entradas, Platos Fuertes, Postres, Bebidas, Bebidas Alcohólicas)
- 11 Products con precios asignados via BranchProduct
- 2 FooterLinks (Instagram, WhatsApp) para la Branch de demo

---

## Application/DTOs

Organizados por operación:

### Read DTOs *(respuestas)*
- `CompanyReadDto`, `BranchReadDto`, `AppUserReadDto`
- `CategoryReadDto` (incluye `Translations[]`)
- `ProductReadDto` (incluye `Tags[]`, `Translations[]`)
- `BranchProductReadDto` (incluye datos del Product base)
- `TagReadDto` (incluye `Translations[]`)
- `CompanyInfoReadDto`, `CompanyThemeReadDto`, `CompanySeoReadDto`
- `BranchLocaleReadDto`, `BranchScheduleReadDto`, `BranchSpecialDayReadDto`
- `ReservationReadDto`
- `CompanySettingsReadDto` (agrega Info + Theme + Seo)
- `BranchSettingsReadDto` (agrega Locale + Form)
- `MenuBranchDto` (composición completa del menú público)

### Create DTOs
- `CategoryCreateDto` con `CategoryTranslationCreateDto[]`
- `ProductCreateDto` con `ProductTranslationCreateDto[]`, `TagIds[]`
- `BranchProductCreateDto` (activa producto en branch con precio)
- `ReservationCreateDto`
- `AppUserCreateDto`, `BranchCreateDto`
- `LoginRequestDto` → `LoginResponseDto` (retorna `token`, `expiresAt`, datos del usuario)

### Update DTOs
- Versiones Update de cada entidad
- `ChangePasswordDto`, `ForgotPasswordDto`, `ResetPasswordDto`
- `CompanyInfoUpdateDto`, `CompanyThemeUpdateDto`, `CompanySeoUpdateDto`
- `BranchLocaleUpdateDto`

---

## Application/Common — AutoMapperProfiles

Todos los mapeos están en `AutoMapperProfiles.cs`. Grupos principales:

| Grupo | Mapeos |
|---|---|
| **Auth** | `Company` → `CompanyReadDto` |
| **Branch** | `Branch` ↔ DTOs |
| **Product** | `Product` ↔ DTOs, `BranchProduct` ↔ DTOs |
| **Category** | `Category` ↔ DTOs, `CategoryTranslation` ↔ DTOs |
| **Tag** | `Tag` ↔ DTOs, `TagTranslation` ↔ DTOs |
| **Settings** | `CompanyInfo/Theme/Seo` ↔ DTOs, `BranchLocale` ↔ DTOs |
| **Reservation** | `Reservation` ↔ DTOs |
| **Modules** | `PlatformModule/CompanyModule` ↔ DTOs |

> Los mapeos complejos (ej: resolver traducciones por idioma, combinar BranchProduct + Product base) se hacen con `AfterMap` o resolvers custom en el perfil.

---

## Flujos de Negocio Principales

### Registro de Company
```
POST /auth/register
  → Valida email/slug únicos
  → Crea Company (con PlanId)
  → Crea CompanyAdmin (BCrypt hash)
  → Crea CompanyInfo/Theme/Seo con defaults
  → Encola email de bienvenida (Outbox)
  → Retorna JWT
```

### Creación de Branch
```
POST /branches
  → Valida MaxBranches del plan
  → Crea Branch
  → BranchLocaleInitializer → crea BranchLocale
  → BranchScheduleInitializer → crea 7 BranchSchedule (L-D)
  → Crea BranchReservationForm con defaults
```

### Activación de Producto en Branch
```
POST /products/branch/{branchId}
  → Verifica que el Product pertenece a la Company
  → Crea BranchProduct con precio y orden
  → El menú público mostrará el producto en esa Branch
```

### Menú Público
```
GET /menu/{slug}
  → Output Cache (evita consultas repetidas)
  → ResolveBySlugAsync → obtiene Company por slug
  → Carga BranchProducts activos con sus Products
  → Aplica traducciones según Accept-Language
  → Retorna MenuBranchDto completo
```

### Reserva de Mesa
```
POST /reservations
  → Verifica módulo RESERVATIONS activo (IModuleGuard)
  → Valida disponibilidad según BranchSchedule
  → Crea Reservation con status Pending
  → Encola email de confirmación al cliente
```

### Reset de Contraseña
```
POST /auth/forgot-password
  → Crea PasswordResetRequest (token GUID, expira en N horas)
  → Encola email con link de reset

GET /auth/validate-token/{token}
  → Verifica token no expirado y no usado

POST /auth/reset-password
  → Valida token → aplica nuevo hash BCrypt → marca token IsUsed=true
```
