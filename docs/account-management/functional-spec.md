# Especificación Funcional: Módulo de Gestión de Cuentas

**Versión:** 1.0
**Última actualización:** 23 de marzo de 2026
**Módulo:** Account Management (DigiMenuAPI)

---

## 1. Descripción General del Módulo

### Propósito
El módulo de Gestión de Cuentas (Cuentas por Cobrar) es el sistema central para que el personal de un restaurante administre los comprobantes de venta del cliente (bills/cuentas). Permite:

- Abrir cuentas nuevas por cliente o identificador
- Agregar artículos del menú a la cuenta con precios capturados en el momento
- Aplicar descuentos predefinidos con flujo de autorización opcional
- Dividir la factura entre múltiples personas o grupos de artículos
- Registrar pagos pendientes como "Tab" (pestaña abierta)
- Cerrar o cancelar cuentas
- Mantener un historial completo de auditoría

### Actores principales
- **Personal de Meseros/Cajas (Staff):** Abre cuentas, agrega artículos, aplica descuentos simples, marca como pagada o tab
- **Administrador de Sucursal (BranchAdmin):** Supervisa personal, autoriza descuentos grandes, cancela cuentas
- **Administrador de Empresa (CompanyAdmin):** Configura catálogo de descuentos, habilita/deshabilita funcionalidad de Tabs
- **SuperAdmin:** Acceso total, auditoría global

### ¿Por qué existe?
En operaciones de restaurante tradicionales, los meseros anotan órdenes en papel y se las pasan a caja. Este módulo digitaliza ese flujo, eliminando errores manuales, mejorando la trazabilidad, y permitiendo descuentos y divisiones de pago en tiempo real.

---

## 2. Glosario de Términos

| Término | Definición |
|---------|-----------|
| **Cuenta (Account)** | Registro de todos los artículos ordenados por un cliente, con subtotal, descuentos y total. Tiene un estado (Abierta, Pendiente de Pago, Cerrada, Cancelada). |
| **Artículo de Cuenta (AccountItem)** | Cada línea en la cuenta: producto, cantidad, precio unitario capturado en el momento. Los descuentos pueden aplicarse a nivel de artículo. |
| **Descuento Predefinido (BranchDiscount)** | Plantilla de descuento creada por CompanyAdmin que define tipo (% o monto fijo), valor, si aplica a toda la cuenta o artículos específicos, y si requiere aprobación. |
| **Descuento en Cuenta (AccountDiscount)** | Aplicación de un descuento predefinido a una cuenta específica, con estado de aprobación (Aprobado, Pendiente, Rechazado). |
| **Snapshot de Precio** | Captura del precio (OfferPrice si existe, sino Price) del producto en el momento que se agrega a la cuenta. Si el precio cambia luego, la cuenta mantiene el precio histórico. |
| **Tab (Pestaña)** | Cuenta marcada como "Pendiente de Pago" cuando el cliente se va sin pagar. El restaurante monitorea ese monto como crédito/deuda. Requiere TabsEnabled=true. |
| **Límite de Crédito de Tab (TabCreditLimit)** | Monto máximo que una sucursal permite en tabs pendientes antes de rechazar nuevos tabs. Opcional. |
| **División de Cuenta (AccountSplit)** | Agrupación de artículos por persona/grupo para dividir el pago. Ej: "Mesa 3 - Juan", "Mesa 3 - María". |
| **Artículo de División (AccountSplitItem)** | Mapeo de cantidades de un artículo a un split específico. Permite dividir un artículo entre personas. |

---

## 3. Flujos de Usuario por Rol

### 3.1 Flujo: Personal (Staff)

**Objetivo:** Tomar orden, aplicar descuentos simples y cerrar venta.

1. **Abrir Cuenta**
   - Staff abre nueva cuenta
   - Ingresa identificador del cliente (Ej: "Mesa 3", "Nombre Cliente", teléfono, etc.)
   - Sistema crea cuenta en estado **Open**

2. **Agregar Artículos**
   - Staff busca producto en catálogo de la sucursal
   - Selecciona cantidad
   - Sistema captura: ProductName, UnitPrice (OfferPrice ?? Price), Quantity
   - Artículo aparece en la cuenta
   - Staff puede agregar más artículos

3. **Aplicar Descuento (sin aprobación)**
   - Staff selecciona descuento predefinido que NO requiere aprobación
   - Ej: "Happy Hour -10%", "Promoción comida -$5"
   - Staff puede aplicar a: toda la cuenta, o a artículos específicos
   - Sistema calcula el descuento y lo aplica automáticamente (Status = Approved)
   - Si el descuento requiere aprobación → Status = PendingApproval → Espera autorización

4. **Aplicar Descuento (requiere aprobación)**
   - Si Staff intenta aplicar descuento con RequiresApproval=true
   - Sistema rechaza o marca como PendingApproval (según política)
   - Staff notifica a BranchAdmin o caja
   - BranchAdmin ve notificación de descuento pendiente, aprueba/rechaza

5. **Cerrar Cuenta (Pago Inmediato)**
   - Staff selecciona "Cerrar Cuenta"
   - Sistema calcula: Subtotal - Descuentos = Total
   - Registra pago (si es pago en efectivo, tarjeta, etc. — nota: pagos están fuera de este módulo)
   - Cuenta cambia a **Closed**

6. **Marcar como Tab (Pago Pendiente)**
   - Si TabsEnabled=true en la sucursal
   - Staff selecciona "Marcar como Tab"
   - Sistema valida límite de crédito (si está configurado)
   - Cuenta cambia a **PendingPayment**
   - TabAuthorizedByUserId = Staff ID (quién lo autorizó)
   - Cliente se va con deuda, se espera pago posterior

### 3.2 Flujo: Administrador de Sucursal (BranchAdmin)

**Objetivo:** Supervisar operaciones, autorizar descuentos grandes, gestionar créditos.

**Incluye todos los permisos de Staff, más:**

1. **Aprobar/Rechazar Descuentos Pendientes**
   - Ve panel de descuentos con Status = PendingApproval
   - Revisa monto, motivo
   - Aprueba o rechaza
   - Sistema actualiza Status y registra quién autorizó

2. **Cancelar Cuenta**
   - Si una cuenta es errónea o fraude
   - Status cambia a **Cancelled**
   - Se registra el cambio en auditoría (ModifiedUserId, ModifiedAt)

3. **Monitorear Tabs Pendientes**
   - Ve lista de cuentas en estado PendingPayment
   - Identifica clientes deudores
   - Puede cambiar status si cliente paga después

4. **Revisar Historial Completo**
   - Accede a detalle de cualquier cuenta
   - Ve todos los descuentos (aprobados y rechazados)
   - Ve auditoría completa (quién creó, quién modificó, cuándo)

### 3.3 Flujo: Administrador de Empresa (CompanyAdmin)

**Objetivo:** Configurar política de descuentos, habilitar Tabs, supervisión global.

**Incluye todos los permisos de BranchAdmin, más:**

1. **Administrar Catálogo de Descuentos (BranchDiscount)**
   - Crear descuentos nuevos:
     - Nombre (Ej: "Descuento Happy Hour")
     - Tipo: % o Monto Fijo
     - Valor por defecto (Ej: 10% o $5)
     - Aplica a: Toda la Cuenta / Artículos Específicos / Ambos
     - ¿Requiere aprobación? Sí/No
     - Límite máximo para Staff (opcional, Ej: max 15%)
     - ¿Está activo? Sí/No
   - Editar descuentos existentes
   - Desactivar/activar descuentos (ej: desactivar promoción vencida)
   - Ver historial de uso de cada descuento

2. **Configurar Funcionalidad de Tabs (Pestaña)**
   - **A nivel de Empresa (CompanyInfo):**
     - TabsEnabled: Habilitar/deshabilitar Tabs globalmente
     - Si está deshabilitado, ninguna sucursal puede crear Tabs
   - **A nivel de Sucursal (BranchLocale):**
     - TabsEnabled: Habilitar/deshabilitar para esta sucursal específica
     - TabCreditLimit: Límite máximo de crédito en tabs pendientes
     - Ej: Max $500 en tabs abiertos antes de rechazar nuevos

3. **Supervisión Global**
   - Reportes: Total de cuentas abiertas/cerradas/tabs pendientes por sucursal
   - Análisis de descuentos más usados
   - Detección de fraude (Staff aplicando muchos descuentos)

### 3.4 Flujo: SuperAdmin

**Objetivo:** Control total, auditoría, configuración de seguridad.

- Acceso a TODO
- Puede cambiar cualquier dato histórico (auditar)
- Puede resetear configuración a nivel de empresa

---

## 4. Características: Sistema de Tabs (Pestaña)

### ¿Qué es un Tab?
Una "pestaña" es un registro de deuda abierta. El cliente se va sin pagar, y el restaurante lo sigue en el sistema.

### Estados de un Tab
1. **Abierto (PendingPayment):** Cliente debe dinero, no ha pagado
2. **Cerrado:** Cliente pagó la deuda, o se condonó
3. **Cancelado:** Se anuló por error o disputa

### Configuración de Tabs

#### Nivel de Empresa (CompanyInfo.TabsEnabled)
- **true:** Todas las sucursales pueden crear Tabs (si la sucursal también lo habilita)
- **false:** Ninguna sucursal puede crear Tabs, incluso si lo intenta

#### Nivel de Sucursal (BranchLocale.TabsEnabled + TabCreditLimit)
- **TabsEnabled = false:** Esta sucursal NO crea Tabs, aunque la empresa los permita
- **TabsEnabled = true:** Esta sucursal puede crear Tabs
- **TabCreditLimit = null:** Sin límite, los Tabs no están restringidos por monto
- **TabCreditLimit = $500:** Máximo $500 en Tabs abiertos. Si hay $480, solo puede aceptar otro Tab de max $20

### Ejemplo de Configuración

**Restaurante Pequeño (sin crédito):**
```
CompanyInfo.TabsEnabled = false
→ Staff NO puede marcar como Tab
→ Todos pagan en el momento
```

**Restaurante Mediano (crédito controlado):**
```
CompanyInfo.TabsEnabled = true
BranchLocale.TabsEnabled = true
BranchLocale.TabCreditLimit = $300
→ Staff puede crear Tabs hasta $300 total abiertos
```

**Restaurante Grande (multi-sucursal):**
```
CompanyInfo.TabsEnabled = true

Sucursal Centro:
  TabsEnabled = true
  TabCreditLimit = $2000

Sucursal Aeropuerto:
  TabsEnabled = false
  (Sin Tabs, solo efectivo/tarjeta)
```

### Validaciones
- Al crear un Tab, sistema verifica: CompanyInfo.TabsEnabled && BranchLocale.TabsEnabled
- Si TabCreditLimit está set, suma todos los Tabs abiertos + el nuevo Tab, debe ser <= TabCreditLimit
- Si excede, rechaza con mensaje claro: "Límite de crédito alcanzado. Límite: $500, Actual: $480, Solicitado: $50"

---

## 5. Sistema de Descuentos

### Tipos de Descuentos

#### 1. Descuentos Predefinidos (BranchDiscount)
El CompanyAdmin crea plantillas reusables:

| Descuento | Tipo | Valor | Aplica a | Requiere Aprobación | Max para Staff |
|-----------|------|-------|----------|---------------------|----------------|
| Happy Hour | % | 10 | Toda Cuenta | No | — |
| Promo Comida | $ | 5 | Artículos Específicos | No | — |
| Descuento VIP | % | 20 | Toda Cuenta | Sí | 15% |
| Cortesía | $ | Flexible | Ambos | Sí | — |

**Explícitamente:**
- **Aplica a Toda Cuenta:** Descuento se aplica al subtotal total
- **Aplica a Artículos Específicos:** Se elige qué artículos descuentan
- **Aplica a Ambos:** Flexible, Staff decide en el momento
- **RequiresApproval:** Si true, Staff no puede aplicar solo, necesita BranchAdmin+
- **MaxValueForStaff:** Límite que Staff puede aplicar. Ej: "Descuento VIP es 20%, pero Staff solo hasta 15%"

#### 2. Descuentos en Cuenta (AccountDiscount)
Cuando Staff aplica un descuento a una cuenta específica:

```
ApplyDiscount(
  AccountId = 123,
  BranchDiscountId = 5,        // Referencia a plantilla
  AccountItemId = null,         // Si null, se aplica a toda la cuenta
  DiscountValue = 10,           // Override del valor por defecto
  Reason = "Cliente habitual"   // Por qué se aplicó
)
```

El descuento toma Status = Approved (si no requiere aprobación) o PendingApproval.

### Cálculo Total (Lado Servidor)

```
itemSubtotal = SUM(item.Quantity * item.UnitPrice) para cada artículo

itemDiscounts = SUM(descuentos aprobados a nivel de artículo)
afterItemDisc = itemSubtotal - itemDiscounts

accountDiscounts = SUM(descuentos aprobados a nivel de cuenta)
total = MAX(0, afterItemDisc - accountDiscounts)
```

### Ejemplo Numérico

**Cuenta de Ejemplo:**

| Artículo | Cantidad | Precio Unit | Subtotal |
|----------|----------|-------------|----------|
| Pasta | 2 | $10 | $20 |
| Vino | 1 | $25 | $25 |
| Postre | 1 | $8 | $8 |
| **Subtotal** | | | **$53** |

**Descuentos Aplicados:**

1. **Descuento de Artículo:** Postre -$3 (Cupón de 25% off en postres)
   - afterItemDisc = $53 - $3 = $50

2. **Descuento de Cuenta:** -10% ($5)
   - total = MAX(0, $50 - $5) = **$45**

**Resultado Final:**
```
Subtotal:           $53.00
Descuento Postre:   -$3.00
Descuento Cuenta:   -$5.00
————————————————————
Total:              $45.00
```

### Flujo de Autorización

```
Staff aplica descuento que requiere aprobación
         ↓
Status = PendingApproval
         ↓
BranchAdmin ve notificación
         ↓
BranchAdmin revisa monto, razón
         ↓
BranchAdmin aprueba (Status = Approved)
  ↓ O rechaza (Status = Rejected)
         ↓
Si rechaza: Descuento no se cuenta en Total
Si aprueba: Se suma al total descuentos
```

---

## 6. División de Cuentas (Bill Splitting)

### Uso Cases
- Mesa de 4 personas que quieren dividir la cuenta
- Cada persona paga su parte
- A veces, un artículo se divide entre personas (ej: botella compartida)

### Conceptos

**AccountSplit:** Representa a una persona/grupo en la división
- Nombre: "Juan", "María", "Grupo A"
- Pertenece a una Account

**AccountSplitItem:** Mapea cantidad de un artículo a un split
- Ej: "De los 2 Pastas, María se lleva 1"
- Cantidad (decimal): Permite fracciones (Ej: 0.5 botella)

### Ejemplo de División

**Cuenta Original (2 personas comparten):**
```
- Pasta Carbonara x2 @ $12 = $24
- Ensalada x1 @ $8 = $8
- Botella Vino x1 @ $30 = $30
Total = $62
```

**División:**
```
Split "Juan":
  - Pasta x1 → $12
  - Botella Vino x0.5 → $15
  Subtotal = $27

Split "María":
  - Pasta x1 → $12
  - Ensalada x1 → $8
  - Botella Vino x0.5 → $15
  Subtotal = $35
```

### Validaciones
1. **Cantidad Total:** Para cada artículo, suma de Split quantities = artículo original
   - Ej: 1 + 1 = 2 pastas (ok), si alguien agrega 0.5 extra → Error
2. **Descuentos:** Se distribuyen proporcionalmente por split (nota: cálculo en frontend es aproximado, backend valida)
3. **No se puede splitear artículos ya divididos:** AccountSplitItem → AccountItem → AccountDiscount (artículo específico) debe estar bien mapppeado

---

## 7. Matriz de Permisos (Lenguaje Natural)

### Abrir/Ver Cuentas
- **Staff:** Sí (propias y de su sucursal)
- **BranchAdmin:** Sí (todas de su sucursal)
- **CompanyAdmin:** Sí (todas de todas sucursales de su empresa)
- **SuperAdmin:** Sí (todas globales)

### Agregar/Quitar Artículos
- **Staff:** Sí (cuentas abiertas)
- **BranchAdmin:** Sí (todas)
- **CompanyAdmin:** Sí (todas)
- **SuperAdmin:** Sí (todas)

### Aplicar Descuento (sin aprobación)
- **Staff:** Sí (si BranchDiscount.RequiresApproval = false)
- **BranchAdmin:** Sí (todos)
- **CompanyAdmin:** Sí (todos)
- **SuperAdmin:** Sí (todos)

### Aplicar Descuento (con aprobación requerida)
- **Staff:** No (rechazado o PendingApproval)
- **BranchAdmin:** Sí (aprobación automática)
- **CompanyAdmin:** Sí (aprobación automática)
- **SuperAdmin:** Sí (aprobación automática)

### Aprobar/Rechazar Descuentos Pendientes
- **Staff:** No
- **BranchAdmin:** Sí (descuentos de su sucursal)
- **CompanyAdmin:** Sí (todos)
- **SuperAdmin:** Sí (todos)

### Marcar como PendingPayment (Tab)
- **Staff:** Sí (si TabsEnabled=true && TabCreditLimit respetado)
- **BranchAdmin:** Sí
- **CompanyAdmin:** Sí
- **SuperAdmin:** Sí

### Cerrar Cuenta
- **Staff:** Sí
- **BranchAdmin:** Sí
- **CompanyAdmin:** Sí
- **SuperAdmin:** Sí

### Cancelar Cuenta
- **Staff:** No (error, data corruption prevention)
- **BranchAdmin:** Sí (su sucursal)
- **CompanyAdmin:** Sí (todas)
- **SuperAdmin:** Sí (todas)

### Gestionar Catálogo de Descuentos
- **Staff:** No
- **BranchAdmin:** No (CompanyAdmin centralizadamente)
- **CompanyAdmin:** Sí
- **SuperAdmin:** Sí

### Configurar Tabs Settings
- **Staff:** No
- **BranchAdmin:** No
- **CompanyAdmin:** Sí (empresa y sucursales)
- **SuperAdmin:** Sí

---

## 8. Guía de Configuración

### Escenario 1: Restaurante Pequeño (20-30 cubiertos)
**Perfil:** Familiar, sin crédito, todo efectivo/tarjeta inmediato

**Configuración Recomendada:**
```
CompanyInfo.TabsEnabled = false

Descuentos Predefinidos:
- "Promo de Hoy" (10% toda cuenta, sin aprobación)
- "Senior/Estudiante" (5% toda cuenta, sin aprobación)

Staff:
- Solo cierra cuentas (Closed), sin Tabs
- Descuentos simples autorizados
```

**Flujo Típico:**
1. Mesero abre cuenta
2. Agrega artículos
3. Si cliente pide descuento, aplica "Promo de Hoy" (automático)
4. Cobra en el momento
5. Cuenta → Closed

### Escenario 2: Restaurante Mediano (50-100 cubiertos, crédito limitado)
**Perfil:** Corporativo con clientes frecuentes, permite crédito pequeño

**Configuración Recomendada:**
```
CompanyInfo.TabsEnabled = true

BranchLocale.TabsEnabled = true
BranchLocale.TabCreditLimit = $500

Descuentos Predefinidos:
- "Happy Hour" (15% artículos bebida, sin aprobación)
- "Cliente VIP" (20% toda cuenta, SÍ requiere aprobación, max 15% para Staff)
- "Cortesía Gerente" (monto flexible, SÍ requiere aprobación)

Roles:
- Staff: Abre, agrega artículos, aplica Happy Hour/cupones, marca Tab si crédito ok
- BranchAdmin: Aprueba descuentos VIP, monitorea tabs
```

**Flujo Típico:**
1. Mesero abre cuenta "Cliente Corporativo ABC"
2. Agrega artículos
3. Cliente pide descuento VIP (20%):
   - Mesero lo aplica → Status = PendingApproval
   - Se notifica al gerente
   - Gerente aprueba o rechaza
4. Cliente paga → Closed, O se va sin pagar → PendingPayment (Tab)
5. Gerente monitorea tabs pendientes

### Escenario 3: Restaurante Grande, Multi-sucursal (150+ cubiertos por sucursal)
**Perfil:** Cadena con múltiples ubicaciones, políticas por ubicación

**Configuración Recomendada:**
```
CompanyInfo.TabsEnabled = true

Sucursal "Centro" (negocio corporativo):
  TabsEnabled = true
  TabCreditLimit = $2000

Sucursal "Aeropuerto" (solo turistas, sin crédito):
  TabsEnabled = false
  TabCreditLimit = null

Sucursal "Nightclub" (eventos):
  TabsEnabled = true
  TabCreditLimit = $5000

Descuentos Globales:
- "Grupo 10+" (20% toda cuenta, requiere aprobación)
- "Combo Ejecutivo" (15%, sin aprobación)
- "Corporate Booking" (variable, requiere aprobación, max 25% para BranchAdmin)

Auditoría:
- CompanyAdmin revisa descuentos por sucursal
- Identifica patrones de fraude
```

---

## 9. Limitaciones Conocidas (v1.0)

### No Incluido en Este Módulo
1. **Módulo de Pagos:** Este módulo NO registra pagos reales (efectivo, tarjeta, transferencia). Solo marca "Closed" o "PendingPayment". El módulo de Pagos (futuro) integrará con procesadores de pago.

2. **Asignación de Mesas:** Las cuentas usan "ClientIdentifier" (texto libre). No hay vínculos automáticos a números de mesa. El mesero ingresa "Mesa 3" manualmente.

3. **Historial de Cambios Detallado:** El módulo registra quién creó/modificó (CreatedUserId, ModifiedUserId) pero no cada cambio específico. Ej: no se ve "artículo eliminado a las 3:45 PM", solo "modificado".

4. **Cierre de Caja (Z-Reports):** No hay reporte de cierre diario. Eso será un módulo separado.

5. **Devoluciones/Cambios:** No hay flujo de "cliente devuelve un artículo". Se asume que Staff quita el artículo manualmente.

6. **Impuestos:** Los precios incluyen impuestos. No hay desglose de IVA/impuestos por artículo.

---

## 10. Hoja de Ruta Futura

### Fase 2 (Q2 2026)
- **Módulo de Pagos:** Integración con gateway de pago, historial de transacciones
- **Cierre de Caja:** Z-Reports, reporte de mesero
- **Notificaciones Tiempo Real:** Push de descuentos pendientes

### Fase 3 (Q3 2026)
- **Asignación de Mesas:** Cuentas ligadas a números de mesa físicos
- **Historial de Cambios Granular:** Ver cada cambio (quién, qué, cuándo)
- **Devoluciones/Cambios:** Flujo de devolución de artículos

### Fase 4 (Q4 2026)
- **Perfiles de Cliente:** Historial de compras, descuentos aplicados
- **Reportes Analíticos:** Top productos, descuentos más usados, análisis de ingresos
- **Promociones Automáticas:** Reglas de descuento condicionales (Ej: "si total > $50, aplica 5%")

---

## 11. Glosario de Estados

### Estado de Cuenta (AccountStatus)
```
Open           = 1  → Cuenta nueva, añadir artículos
PendingPayment = 2  → Tab abierto, cliente adeuda
Closed         = 3  → Pagado y finalizado
Cancelled      = 4  → Cancelado por error/fraude
```

### Estado de Descuento (AccountDiscountStatus)
```
Approved       = 1  → Autorizado, se suma al total de descuentos
PendingApproval = 2  → Espera autorización del BranchAdmin
Rejected       = 3  → Rechazado, no se aplica
```

### Tipo de Descuento (DiscountType)
```
Percentage   = 1  → % del valor (Ej: 10%)
FixedAmount  = 2  → Monto fijo (Ej: $5)
```

### Descuento Aplica A (DiscountAppliesTo)
```
WholeAccount  = 1  → A toda la cuenta
SpecificItem  = 2  → A artículos específicos
Both          = 3  → Flexible (Staff elige)
```

---

## 12. Consideraciones de UX

### Para Staff (Punto de Venta)
- **Simplificar flujo:** Abrir → Agregar → Descuento → Cerrar
- **Búsqueda rápida de productos:** Autocomplete, código de barras
- **Validación en tiempo real:** "Tab excede límite de crédito"
- **Atajos de teclado:** F5 descuentos, F9 cerrar cuenta

### Para BranchAdmin (Supervisión)
- **Notificaciones claras:** "3 descuentos pendientes de aprobación"
- **Dashboard:** Tabs abiertos, descuentos por aprobar, tendencias
- **Acceso rápido:** Buscar cuenta por nombre/mesa

### Para CompanyAdmin (Configuración)
- **Asistente de descuentos:** Step-by-step para crear nuevos
- **Validación de límites:** "Advertencia: MaxValueForStaff es mayor que DefaultValue"
- **Previsualización:** Simular descuento antes de guardar

---

## 13. Datos de Referencia

### Códigos de Error (ErrorKeys)
```
ACCOUNT_NOT_FOUND          → Cuenta no existe
BRANCH_NOT_OWNED           → Sucursal no pertenece al usuario
INSUFFICIENT_PERMISSIONS   → Rol no tiene permiso
DISCOUNT_REQUIRES_APPROVAL → Descuento requiere aprobación
TAB_CREDIT_LIMIT_EXCEEDED  → Límite de Tab excedido
ACCOUNT_NOT_OPEN           → Cuenta no está en estado Open
DISCOUNT_ALREADY_EXISTS    → Descuento ya aplicado
INVALID_SPLIT_QUANTITY     → Cantidad de split no cuadra
```

### Mensajes Amigables
```
"Cuenta creada correctamente."
"Artículo agregado."
"Descuento pendiente de aprobación. El gerente será notificado."
"Límite de crédito alcanzado. Solicitud rechazada."
"Cuenta cerrada. ¡Gracias por su compra!"
"Tab registrado. Total adeudado: $45.50"
```

---

**Fin de la Especificación Funcional**
