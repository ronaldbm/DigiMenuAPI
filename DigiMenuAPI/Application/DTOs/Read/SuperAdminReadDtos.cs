using AppCore.Domain.Entities;

namespace DigiMenuAPI.Application.DTOs.Read
{
    // ── Tenant / Company ──────────────────────────────────────────────

    /// <summary>Vista resumida para la tabla de tenants en el panel SuperAdmin.</summary>
    public record TenantSummaryDto(
        int Id,
        string Name,
        string Slug,
        string Email,
        string? Phone,
        string? CountryCode,
        bool IsActive,
        int PlanId,
        string PlanName,
        string PlanCode,
        int MaxBranches,
        int MaxUsers,
        int BranchCount,
        int UserCount,
        SubscriptionStatus? SubscriptionStatus,
        DateTime? SubscriptionEndDate,
        DateTime? LastPaymentDate,
        decimal? LastPaymentAmount,
        DateTime CreatedAt
    );

    /// <summary>Vista completa de un tenant para la página de detalle.</summary>
    public record TenantDetailDto(
        int Id,
        string Name,
        string Slug,
        string Email,
        string? Phone,
        string? CountryCode,
        bool IsActive,
        int PlanId,
        string PlanName,
        string PlanCode,
        int MaxBranches,
        int MaxUsers,
        int BranchCount,
        int UserCount,
        SubscriptionDto? Subscription,
        List<PaymentRecordDto> RecentPayments,
        List<TenantBranchSummaryDto> Branches,
        List<TenantUserSummaryDto> Users,
        DateTime CreatedAt
    );

    public record TenantBranchSummaryDto(
        int Id,
        string Name,
        string Slug,
        bool IsActive
    );

    public record TenantUserSummaryDto(
        int Id,
        string FullName,
        string Email,
        byte Role,
        bool IsActive,
        DateTime? LastLoginAt
    );

    // ── Subscription ──────────────────────────────────────────────────

    public record SubscriptionDto(
        int Id,
        int CompanyId,
        string CompanyName,
        string CompanySlug,
        int PlanId,
        string PlanName,
        SubscriptionStatus Status,
        string StatusLabel,
        DateTime StartDate,
        DateTime EndDate,
        DateTime? NextBillingDate,
        DateTime? TrialEndsAt,
        DateTime? SuspendedAt,
        string? SuspendedReason,
        string? Notes,
        int DaysUntilExpiry,   // negativo = ya venció
        DateTime CreatedAt
    );

    // ── Payment ───────────────────────────────────────────────────────

    public record PaymentRecordDto(
        int Id,
        int CompanyId,
        string CompanyName,
        int SubscriptionId,
        decimal Amount,
        string Currency,
        DateTime PaidAt,
        PaymentMethod Method,
        string MethodLabel,
        string? Reference,
        string? Notes,
        PaymentStatus Status,
        string StatusLabel,
        string RecordedByFullName,
        DateTime CreatedAt
    );

    // ── Dashboard Metrics ─────────────────────────────────────────────

    public record DashboardMetricsDto(
        int TotalTenants,
        int ActiveTenants,
        int TrialTenants,
        int ExpiringIn30Days,
        int ExpiredTenants,
        int SuspendedTenants,
        decimal TotalMrrUsd,           // suma de MonthlyPrice de suscripciones activas
        int NewTenantsThisMonth,
        int AtRiskCount,               // vencidos o suspendidos sin pago reciente
        List<PaymentRecordDto> RecentPayments,
        List<TenantSummaryDto> AtRiskTenants
    );

    // ── Plan (lectura para SuperAdmin, incluye datos que no van al público) ──

    public record PlanAdminDto(
        int Id,
        string Code,
        string Name,
        string? Description,
        decimal MonthlyPrice,
        decimal? AnnualPrice,
        int MaxBranches,
        int MaxUsers,
        bool IsPublic,
        bool IsActive,
        int DisplayOrder,
        int ActiveTenantCount  // cuántos tenants usan este plan
    );

    // ── Impersonation ─────────────────────────────────────────────────

    /// <summary>Token de un solo uso retornado al SuperAdmin para abrir sesión en el tenant.</summary>
    public record ImpersonationTokenDto(
        string Token,          // token en claro — uso único, válido 30 minutos
        int TargetCompanyId,
        string TargetCompanyName,
        string TargetCompanySlug,
        DateTime ExpiresAt
    );
}
