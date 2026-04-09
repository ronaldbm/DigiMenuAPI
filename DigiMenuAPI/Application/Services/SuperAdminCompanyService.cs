using AppCore.Application.Common;
using AppCore.Application.DTOs.Email;
using AppCore.Application.Interfaces;
using AppCore.Application.Utils;
using AppCore.Domain.Entities;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services
{
    /// <summary>
    /// Gestión de tenants desde la perspectiva del SuperAdmin.
    /// Excluye siempre la empresa maestra (Id=1, "DigiMenu Platform") de todos los listados.
    /// </summary>
    public class SuperAdminCompanyService : ISuperAdminCompanyService
    {
        // La empresa maestra nunca debe aparecer en los listados de tenants
        private const int MasterCompanyId = 1;

        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly IEmailQueueService _emailQueue;
        private readonly IConfiguration _config;

        private string AppUrl => _config["Email:AppUrl"] ?? "https://app.digimenu.cr";

        public SuperAdminCompanyService(
            ApplicationDbContext context,
            ITenantService tenantService,
            IEmailQueueService emailQueue,
            IConfiguration config)
        {
            _context = context;
            _tenantService = tenantService;
            _emailQueue = emailQueue;
            _config = config;
        }

        // ── GET ALL ───────────────────────────────────────────────────
        public async Task<OperationResult<List<TenantSummaryDto>>> GetAll(
            string? search, int? planId, string? status, int page, int pageSize)
        {
            // Limitar tamaño de página para proteger el servidor
            pageSize = Math.Clamp(pageSize, 1, 100);
            page = Math.Max(1, page);

            var query = _context.Companies
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(c => c.Id != MasterCompanyId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(s) ||
                    c.Slug.ToLower().Contains(s) ||
                    c.Email.ToLower().Contains(s));
            }

            if (planId.HasValue)
                query = query.Where(c => c.PlanId == planId.Value);

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<SubscriptionStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(c =>
                    c.Subscription != null && c.Subscription.Status == parsedStatus);
            }

            var companies = await query
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    c.Id, c.Name, c.Slug, c.Email, c.Phone, c.CountryCode,
                    c.IsActive, c.PlanId, c.MaxBranches, c.MaxUsers, c.CreatedAt,
                    PlanName = c.Plan != null ? c.Plan.Name : "—",
                    PlanCode = c.Plan != null ? c.Plan.Code : "—",
                    BranchCount = c.Branches.Count(b => !b.IsDeleted),
                    UserCount = c.Users.Count(u => !u.IsDeleted),
                    Sub = c.Subscription,
                    LastPayment = c.Subscription != null
                        ? c.Subscription.Payments
                            .OrderByDescending(p => p.PaidAt)
                            .FirstOrDefault()
                        : null
                })
                .ToListAsync();

            var result = companies.Select(c => new TenantSummaryDto(
                c.Id, c.Name, c.Slug, c.Email, c.Phone, c.CountryCode,
                c.IsActive, c.PlanId, c.PlanName, c.PlanCode,
                c.MaxBranches, c.MaxUsers, c.BranchCount, c.UserCount,
                c.Sub?.Status,
                c.Sub?.EndDate,
                c.LastPayment?.PaidAt,
                c.LastPayment?.Amount,
                c.CreatedAt
            )).ToList();

            return OperationResult<List<TenantSummaryDto>>.Ok(result);
        }

        // ── GET BY ID ─────────────────────────────────────────────────
        public async Task<OperationResult<TenantDetailDto>> GetById(int companyId)
        {
            var company = await _context.Companies
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(c => c.Id == companyId && c.Id != MasterCompanyId)
                .Include(c => c.Plan)
                .Include(c => c.Subscription)
                    .ThenInclude(s => s!.Plan)
                .Include(c => c.Subscription)
                    .ThenInclude(s => s!.Payments.OrderByDescending(p => p.PaidAt).Take(10))
                        .ThenInclude(p => p.RecordedBy)
                .Include(c => c.Branches.Where(b => !b.IsDeleted))
                .Include(c => c.Users.Where(u => !u.IsDeleted))
                .FirstOrDefaultAsync();

            if (company is null)
                return OperationResult<TenantDetailDto>.NotFound(
                    "Tenant no encontrado.",
                    ErrorKeys.CompanyNotFound);

            return OperationResult<TenantDetailDto>.Ok(MapToDetail(company));
        }

        // ── CREATE TENANT ─────────────────────────────────────────────
        public async Task<OperationResult<TenantDetailDto>> Create(CreateTenantDto dto)
        {
            var slug = dto.Slug.Trim().ToLower();
            var companyEmail = dto.CompanyEmail.Trim().ToLower();
            var adminEmail = dto.AdminEmail.Trim().ToLower();

            // Validar unicidad de slug (es el subdominio)
            if (await _context.Companies.IgnoreQueryFilters()
                    .AnyAsync(c => c.Slug == slug))
                return OperationResult<TenantDetailDto>.Conflict(
                    $"El subdominio '{slug}.digimenu.cr' ya está en uso.",
                    ErrorKeys.SlugAlreadyExists);

            // Validar que el email del admin no esté en uso
            if (await _context.Users.IgnoreQueryFilters()
                    .AnyAsync(u => u.Email == adminEmail))
                return OperationResult<TenantDetailDto>.Conflict(
                    "El email del administrador ya está registrado.",
                    ErrorKeys.EmailAlreadyExists);

            // Validar que el plan existe
            var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == dto.PlanId);
            if (plan is null)
                return OperationResult<TenantDetailDto>.NotFound(
                    "Plan no encontrado.",
                    ErrorKeys.PlanNotFound);

            // Determinar límites: usar los del plan salvo que se especifiquen custom
            var maxBranches = dto.CustomMaxBranches ?? plan.MaxBranches;
            var maxUsers = dto.CustomMaxUsers ?? plan.MaxUsers;

            // 1. Crear Company
            var company = new Company
            {
                Name = dto.CompanyName.Trim(),
                Slug = slug,
                Email = companyEmail,
                Phone = dto.Phone?.Trim(),
                CountryCode = dto.CountryCode?.Trim().ToUpper(),
                IsActive = true,
                PlanId = dto.PlanId,
                MaxBranches = maxBranches,
                MaxUsers = maxUsers
            };
            _context.Companies.Add(company);
            await _context.SaveChangesAsync(); // Necesitamos company.Id

            // 2. Crear empresa config mínima (Info + Theme + Seo)
            _context.CompanyInfos.Add(new CompanyInfo
            {
                CompanyId = company.Id,
                BusinessName = company.Name
            });
            _context.CompanySeos.Add(new CompanySeo { CompanyId = company.Id });
            _context.CompanyLanguages.Add(new CompanyLanguage
            {
                CompanyId = company.Id,
                LanguageCode = "es",
                IsDefault = true
            });

            // 3. Crear Subscription
            var subscription = new Subscription
            {
                CompanyId = company.Id,
                PlanId = dto.PlanId,
                Status = dto.InitialStatus,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Notes = dto.SubscriptionNotes
            };
            _context.Subscriptions.Add(subscription);

            // 4. Crear CompanyAdmin con contraseña temporal
            var tempPassword = PasswordValidator.GenerateTemporary();
            var admin = new AppUser
            {
                CompanyId = company.Id,
                BranchId = null,
                FullName = dto.AdminFullName.Trim(),
                Email = adminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
                Role = UserRoles.CompanyAdmin,
                IsActive = true,
                MustChangePassword = true
            };
            _context.Users.Add(admin);

            await _context.SaveChangesAsync();

            // 5. Encolar email de bienvenida con contraseña temporal
            await _emailQueue.QueueTemporaryPasswordAsync(new TemporaryPasswordEmailDto(
                ToEmail: adminEmail,
                FullName: dto.AdminFullName.Trim(),
                CompanyName: dto.CompanyName.Trim(),
                TemporaryPassword: tempPassword,
                LoginUrl: $"{AppUrl}/admin/login"
            ), company.Id);

            // Cargar relaciones para devolver el detalle completo
            var created = await _context.Companies
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(c => c.Plan)
                .Include(c => c.Subscription).ThenInclude(s => s!.Plan)
                .Include(c => c.Branches.Where(b => !b.IsDeleted))
                .Include(c => c.Users.Where(u => !u.IsDeleted))
                .FirstAsync(c => c.Id == company.Id);

            return OperationResult<TenantDetailDto>.Ok(MapToDetail(created));
        }

        // ── UPDATE INFO ───────────────────────────────────────────────
        public async Task<OperationResult<TenantSummaryDto>> UpdateInfo(
            int companyId, UpdateCompanyInfoDto dto)
        {
            var company = await _context.Companies
                .IgnoreQueryFilters()
                .Include(c => c.Plan)
                .FirstOrDefaultAsync(c => c.Id == companyId && c.Id != MasterCompanyId);

            if (company is null)
                return OperationResult<TenantSummaryDto>.NotFound(
                    "Tenant no encontrado.",
                    ErrorKeys.CompanyNotFound);

            var newSlug = dto.Slug.Trim().ToLower();
            if (newSlug != company.Slug &&
                await _context.Companies.IgnoreQueryFilters()
                    .AnyAsync(c => c.Slug == newSlug && c.Id != companyId))
                return OperationResult<TenantSummaryDto>.Conflict(
                    $"El subdominio '{newSlug}.digimenu.cr' ya está en uso.",
                    ErrorKeys.SlugAlreadyExists);

            company.Name = dto.Name.Trim();
            company.Slug = newSlug;
            company.Email = dto.Email.Trim().ToLower();
            company.Phone = dto.Phone?.Trim();
            company.CountryCode = dto.CountryCode?.Trim().ToUpper();
            company.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();
            return OperationResult<TenantSummaryDto>.Ok(await LoadSummaryAsync(company.Id));
        }

        // ── UPDATE PLAN ───────────────────────────────────────────────
        public async Task<OperationResult<TenantSummaryDto>> UpdatePlan(
            int companyId, UpdateCompanyPlanDto dto)
        {
            var company = await _context.Companies
                .IgnoreQueryFilters()
                .Include(c => c.Plan)
                .FirstOrDefaultAsync(c => c.Id == companyId && c.Id != MasterCompanyId);

            if (company is null)
                return OperationResult<TenantSummaryDto>.NotFound(
                    "Tenant no encontrado.",
                    ErrorKeys.CompanyNotFound);

            var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == dto.PlanId);
            if (plan is null)
                return OperationResult<TenantSummaryDto>.NotFound(
                    "Plan no encontrado.",
                    ErrorKeys.PlanNotFound);

            company.PlanId = dto.PlanId;
            if (dto.ApplyPlanLimits)
            {
                company.MaxBranches = plan.MaxBranches;
                company.MaxUsers = plan.MaxUsers;
            }

            await _context.SaveChangesAsync();
            return OperationResult<TenantSummaryDto>.Ok(await LoadSummaryAsync(company.Id));
        }

        // ── UPDATE LIMITS ─────────────────────────────────────────────
        public async Task<OperationResult<TenantSummaryDto>> UpdateLimits(
            int companyId, UpdateCompanyLimitsDto dto)
        {
            var company = await _context.Companies
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == companyId && c.Id != MasterCompanyId);

            if (company is null)
                return OperationResult<TenantSummaryDto>.NotFound(
                    "Tenant no encontrado.",
                    ErrorKeys.CompanyNotFound);

            company.MaxBranches = dto.MaxBranches;
            company.MaxUsers = dto.MaxUsers;

            await _context.SaveChangesAsync();
            return OperationResult<TenantSummaryDto>.Ok(await LoadSummaryAsync(company.Id));
        }

        // ── TOGGLE ACTIVE ─────────────────────────────────────────────
        public async Task<OperationResult<bool>> ToggleActive(int companyId)
        {
            var company = await _context.Companies
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == companyId && c.Id != MasterCompanyId);

            if (company is null)
                return OperationResult<bool>.NotFound(
                    "Tenant no encontrado.",
                    ErrorKeys.CompanyNotFound);

            company.IsActive = !company.IsActive;
            await _context.SaveChangesAsync();

            return OperationResult<bool>.Ok(company.IsActive);
        }

        // ── Helpers ───────────────────────────────────────────────────

        private async Task<TenantSummaryDto> LoadSummaryAsync(int companyId)
        {
            return await _context.Companies
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(c => c.Id == companyId)
                .Select(c => new TenantSummaryDto(
                    c.Id, c.Name, c.Slug, c.Email, c.Phone, c.CountryCode,
                    c.IsActive, c.PlanId, c.Plan != null ? c.Plan.Name : "—", c.Plan != null ? c.Plan.Code : "—",
                    c.MaxBranches, c.MaxUsers,
                    c.Branches.Count(b => !b.IsDeleted),
                    c.Users.Count(u => !u.IsDeleted),
                    c.Subscription != null ? c.Subscription.Status : null,
                    c.Subscription != null ? c.Subscription.EndDate : null,
                    c.Subscription != null
                        ? c.Subscription.Payments.OrderByDescending(p => p.PaidAt)
                            .Select(p => (DateTime?)p.PaidAt).FirstOrDefault()
                        : null,
                    c.Subscription != null
                        ? c.Subscription.Payments.OrderByDescending(p => p.PaidAt)
                            .Select(p => (decimal?)p.Amount).FirstOrDefault()
                        : null,
                    c.CreatedAt
                ))
                .FirstAsync();
        }

        private static TenantDetailDto MapToDetail(Company c)
        {
            var sub = c.Subscription;
            SubscriptionDto? subDto = sub is null ? null : MapSubscription(sub, c.Name, c.Slug);

            var payments = sub?.Payments
                .OrderByDescending(p => p.PaidAt)
                .Select(p => MapPayment(p, c.Name))
                .ToList() ?? new List<PaymentRecordDto>();

            return new TenantDetailDto(
                c.Id, c.Name, c.Slug, c.Email, c.Phone, c.CountryCode,
                c.IsActive, c.PlanId, c.Plan?.Name ?? "—", c.Plan?.Code ?? "—",
                c.MaxBranches, c.MaxUsers,
                c.Branches.Count,
                c.Users.Count,
                subDto,
                payments,
                c.Branches.Select(b => new TenantBranchSummaryDto(b.Id, b.Name, b.Slug, b.IsActive)).ToList(),
                c.Users.Select(u => new TenantUserSummaryDto(u.Id, u.FullName, u.Email, u.Role, u.IsActive, u.LastLoginAt)).ToList(),
                c.CreatedAt
            );
        }

        internal static SubscriptionDto MapSubscription(Subscription s, string companyName, string companySlug) =>
            new(s.Id, s.CompanyId, companyName, companySlug,
                s.PlanId, s.Plan?.Name ?? string.Empty,
                s.Status, s.Status.ToString(),
                s.StartDate, s.EndDate, s.NextBillingDate,
                s.TrialEndsAt, s.SuspendedAt, s.SuspendedReason, s.Notes,
                (int)(s.EndDate - DateTime.UtcNow).TotalDays,
                s.CreatedAt);

        internal static PaymentRecordDto MapPayment(PaymentRecord p, string companyName) =>
            new(p.Id, p.CompanyId, companyName, p.SubscriptionId,
                p.Amount, p.Currency, p.PaidAt,
                p.Method, p.Method.ToString(),
                p.Reference, p.Notes,
                p.Status, p.Status.ToString(),
                p.RecordedBy?.FullName ?? string.Empty,
                p.CreatedAt);
    }
}
