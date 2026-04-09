using AppCore.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Create
{
    // ── Crear Tenant ──────────────────────────────────────────────────

    /// <summary>
    /// Datos para que el SuperAdmin dé de alta un nuevo tenant.
    /// El sistema crea: Company + CompanyAdmin + Subscription + Company config (info, theme, seo).
    /// La contraseña del admin se genera automáticamente y se envía por email.
    /// </summary>
    public record CreateTenantDto(
        [Required, MaxLength(100)] string CompanyName,

        /// <summary>
        /// Slug único global. Se usará como subdominio: {slug}.digimenu.cr
        /// Solo letras minúsculas, números y guiones. Sin espacios.
        /// </summary>
        [Required, MaxLength(60), RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$",
            ErrorMessage = "El slug solo puede contener letras minúsculas, números y guiones.")]
        string Slug,

        [Required, MaxLength(150), EmailAddress] string CompanyEmail,
        [MaxLength(20)] string? Phone,
        [MaxLength(3)] string? CountryCode,
        int PlanId,
        /// <summary>Límites personalizados. -1 = usar los del plan. -2 = ilimitado.</summary>
        int? CustomMaxBranches,
        int? CustomMaxUsers,

        // ── Datos del primer CompanyAdmin ─────────────────────────────
        [Required, MaxLength(100)] string AdminFullName,
        [Required, MaxLength(150), EmailAddress] string AdminEmail,

        // ── Suscripción inicial ───────────────────────────────────────
        SubscriptionStatus InitialStatus,
        DateTime StartDate,
        DateTime EndDate,
        string? SubscriptionNotes
    );

    // ── Registrar Pago ────────────────────────────────────────────────

    public record RegisterPaymentDto(
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0.")]
        decimal Amount,

        [Required, MaxLength(5)] string Currency,
        DateTime PaidAt,
        PaymentMethod Method,
        [MaxLength(100)] string? Reference,
        [MaxLength(500)] string? Notes,
        PaymentStatus Status
    );

    // ── Actualizar Suscripción ────────────────────────────────────────

    public record UpdateSubscriptionDto(
        SubscriptionStatus Status,
        DateTime EndDate,
        DateTime? NextBillingDate,
        DateTime? TrialEndsAt,
        [MaxLength(500)] string? SuspendedReason,
        [MaxLength(1000)] string? Notes
    );

    // ── Actualizar Límites de Tenant ──────────────────────────────────

    public record UpdateCompanyLimitsDto(
        /// <summary>-1 = ilimitado.</summary>
        int MaxBranches,
        int MaxUsers
    );

    // ── Cambiar Plan de Tenant ────────────────────────────────────────

    public record UpdateCompanyPlanDto(
        int PlanId,
        /// <summary>Si true, aplica los límites del nuevo plan a Company.MaxBranches/MaxUsers.</summary>
        bool ApplyPlanLimits
    );

    // ── Editar Info Básica del Tenant ─────────────────────────────────

    public record UpdateCompanyInfoDto(
        [Required, MaxLength(100)] string Name,

        [Required, MaxLength(60), RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$",
            ErrorMessage = "El slug solo puede contener letras minúsculas, números y guiones.")]
        string Slug,

        [Required, MaxLength(150), EmailAddress] string Email,
        [MaxLength(20)] string? Phone,
        [MaxLength(3)] string? CountryCode,
        bool IsActive
    );

    // ── Crear / Actualizar Plan ───────────────────────────────────────

    public record PlanUpsertDto(
        [Required, MaxLength(50)] string Code,
        [Required, MaxLength(100)] string Name,
        [MaxLength(300)] string? Description,
        [Range(0, double.MaxValue)] decimal MonthlyPrice,
        [Range(0, double.MaxValue)] decimal? AnnualPrice,
        /// <summary>-1 = ilimitado.</summary>
        int MaxBranches,
        int MaxUsers,
        bool IsPublic,
        bool IsActive,
        int DisplayOrder
    );
}
