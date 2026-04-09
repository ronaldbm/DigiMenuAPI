using AppCore.Application.Common;
using AppCore.Domain.Entities;
using DigiMenuAPI.Application.DTOs.Auth;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;

namespace DigiMenuAPI.Application.Interfaces
{
    // ── Gestión de Tenants ────────────────────────────────────────────

    /// <summary>
    /// Gestión completa de tenants (Companies) desde la perspectiva del SuperAdmin.
    /// Todas las operaciones requieren role=255 (SuperAdmin).
    ///
    /// La empresa maestra (Id=1, "DigiMenu Platform") se excluye de todos los listados
    /// y no puede ser modificada por estos endpoints.
    /// </summary>
    public interface ISuperAdminCompanyService
    {
        /// <summary>
        /// Lista todos los tenants con filtros y paginación.
        /// Excluye la empresa maestra (Id=1).
        /// </summary>
        Task<OperationResult<List<TenantSummaryDto>>> GetAll(
            string? search, int? planId, string? status, int page, int pageSize);

        /// <summary>Vista completa de un tenant: datos, suscripción, pagos, branches, usuarios.</summary>
        Task<OperationResult<TenantDetailDto>> GetById(int companyId);

        /// <summary>
        /// Da de alta un nuevo tenant: crea Company, Subscription, CompanyAdmin
        /// con contraseña temporal y envía email de bienvenida.
        /// Devuelve el subdominio que el SuperAdmin debe configurar manualmente en DNS.
        /// </summary>
        Task<OperationResult<TenantDetailDto>> Create(CreateTenantDto dto);

        /// <summary>Actualiza nombre, slug, email, teléfono y estado de un tenant.</summary>
        Task<OperationResult<TenantSummaryDto>> UpdateInfo(int companyId, UpdateCompanyInfoDto dto);

        /// <summary>
        /// Cambia el plan asignado al tenant.
        /// Si ApplyPlanLimits = true, actualiza también Company.MaxBranches y Company.MaxUsers.
        /// </summary>
        Task<OperationResult<TenantSummaryDto>> UpdatePlan(int companyId, UpdateCompanyPlanDto dto);

        /// <summary>Modifica los límites personalizados de branches y usuarios.</summary>
        Task<OperationResult<TenantSummaryDto>> UpdateLimits(int companyId, UpdateCompanyLimitsDto dto);

        /// <summary>
        /// Activa o desactiva un tenant.
        /// Desactivar impide el login de todos sus usuarios.
        /// </summary>
        Task<OperationResult<bool>> ToggleActive(int companyId);
    }

    // ── Gestión de Suscripciones y Pagos ─────────────────────────────

    /// <summary>
    /// Gestión del ciclo de vida de suscripciones y registro manual de pagos.
    /// Todas las operaciones requieren role=255 (SuperAdmin).
    /// </summary>
    public interface ISuperAdminSubscriptionService
    {
        /// <summary>Lista suscripciones con filtros por estado.</summary>
        Task<OperationResult<List<SubscriptionDto>>> GetAll(string? status);

        /// <summary>Suscripción de un tenant específico.</summary>
        Task<OperationResult<SubscriptionDto>> GetByCompany(int companyId);

        /// <summary>Tenants cuya suscripción vence en los próximos N días.</summary>
        Task<OperationResult<List<SubscriptionDto>>> GetExpiringSoon(int days = 30);

        /// <summary>Tenants con suscripción vencida o suspendida (at-risk).</summary>
        Task<OperationResult<List<SubscriptionDto>>> GetAtRisk();

        /// <summary>
        /// Actualiza estado, fecha de vencimiento y notas de una suscripción.
        /// Si el estado es Suspended, registra SuspendedAt y SuspendedReason.
        /// </summary>
        Task<OperationResult<SubscriptionDto>> Update(int companyId, UpdateSubscriptionDto dto);

        /// <summary>Historial de pagos de un tenant.</summary>
        Task<OperationResult<List<PaymentRecordDto>>> GetPayments(int companyId);

        /// <summary>
        /// Registra un pago manual recibido de un tenant.
        /// RecordedByUserId se toma del JWT del SuperAdmin autenticado.
        /// </summary>
        Task<OperationResult<PaymentRecordDto>> RegisterPayment(int companyId, RegisterPaymentDto dto);

        /// <summary>Actualiza el estado de un pago (ej. marcar como Refunded).</summary>
        Task<OperationResult<PaymentRecordDto>> UpdatePaymentStatus(int paymentId, PaymentStatus paymentStatus);
    }

    // ── Gestión de Planes ─────────────────────────────────────────────

    /// <summary>
    /// CRUD de planes de suscripción.
    /// Todas las operaciones requieren role=255 (SuperAdmin).
    /// </summary>
    public interface ISuperAdminPlanService
    {
        Task<OperationResult<List<PlanAdminDto>>> GetAll();
        Task<OperationResult<PlanAdminDto>> GetById(int planId);
        Task<OperationResult<PlanAdminDto>> Create(PlanUpsertDto dto);
        Task<OperationResult<PlanAdminDto>> Update(int planId, PlanUpsertDto dto);
        Task<OperationResult<bool>> ToggleActive(int planId);
    }

    // ── Dashboard ─────────────────────────────────────────────────────

    /// <summary>Métricas globales de la plataforma para el dashboard del SuperAdmin.</summary>
    public interface ISuperAdminDashboardService
    {
        Task<OperationResult<DashboardMetricsDto>> GetMetrics();
    }

    // ── Impersonación ─────────────────────────────────────────────────

    /// <summary>
    /// Emisión y validación de tokens de impersonación.
    ///
    /// Seguridad:
    ///   - Token: 32 bytes aleatorios (RandomNumberGenerator)
    ///   - Almacenado: SHA-256 del token (nunca en claro)
    ///   - TTL: 30 minutos
    ///   - One-time use: InvalidateAsync marca UsedAt en transacción atómica
    ///   - Transmisión: el frontend pasa el token vía URL fragment (no llega al servidor)
    /// </summary>
    public interface ISuperAdminImpersonationService
    {
        /// <summary>
        /// Genera un token de impersonación para el primer CompanyAdmin activo del tenant.
        /// Registra la sesión en ImpersonationSession con el hash del token.
        /// </summary>
        Task<OperationResult<ImpersonationTokenDto>> CreateToken(int companyId);

        /// <summary>
        /// Valida el token, lo marca como usado (one-time use) y devuelve
        /// el JWT del CompanyAdmin del tenant con claim "imp_by" = superAdminUserId.
        /// El exchange es atómico: la validación y el marcado de UsedAt son la misma transacción.
        /// </summary>
        Task<OperationResult<LoginResponseDto>> ExchangeToken(string token);
    }
}
