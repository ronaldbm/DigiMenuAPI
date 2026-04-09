using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services
{
    public class SuperAdminPlanService : ISuperAdminPlanService
    {
        private readonly ApplicationDbContext _context;

        public SuperAdminPlanService(ApplicationDbContext context)
            => _context = context;

        // ── GET ALL ───────────────────────────────────────────────────
        public async Task<OperationResult<List<PlanAdminDto>>> GetAll()
        {
            var plans = await _context.Plans
                .AsNoTracking()
                .OrderBy(p => p.DisplayOrder)
                .Select(p => new PlanAdminDto(
                    p.Id, p.Code, p.Name, p.Description,
                    p.MonthlyPrice, p.AnnualPrice,
                    p.MaxBranches, p.MaxUsers,
                    p.IsPublic, p.IsActive, p.DisplayOrder,
                    _context.Companies.Count(c => c.PlanId == p.Id)
                ))
                .ToListAsync();

            return OperationResult<List<PlanAdminDto>>.Ok(plans);
        }

        // ── GET BY ID ─────────────────────────────────────────────────
        public async Task<OperationResult<PlanAdminDto>> GetById(int planId)
        {
            var plan = await _context.Plans
                .AsNoTracking()
                .Where(p => p.Id == planId)
                .Select(p => new PlanAdminDto(
                    p.Id, p.Code, p.Name, p.Description,
                    p.MonthlyPrice, p.AnnualPrice,
                    p.MaxBranches, p.MaxUsers,
                    p.IsPublic, p.IsActive, p.DisplayOrder,
                    _context.Companies.Count(c => c.PlanId == p.Id)
                ))
                .FirstOrDefaultAsync();

            if (plan is null)
                return OperationResult<PlanAdminDto>.NotFound(
                    "Plan no encontrado.",
                    ErrorKeys.PlanNotFound);

            return OperationResult<PlanAdminDto>.Ok(plan);
        }

        // ── CREATE ────────────────────────────────────────────────────
        public async Task<OperationResult<PlanAdminDto>> Create(PlanUpsertDto dto)
        {
            var code = dto.Code.Trim().ToUpper();
            if (await _context.Plans.AnyAsync(p => p.Code == code))
                return OperationResult<PlanAdminDto>.Conflict(
                    $"Ya existe un plan con el código '{code}'.",
                    ErrorKeys.PlanCodeAlreadyExists);

            var plan = new AppCore.Domain.Entities.Plan
            {
                Code = code,
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                MonthlyPrice = dto.MonthlyPrice,
                AnnualPrice = dto.AnnualPrice,
                MaxBranches = dto.MaxBranches,
                MaxUsers = dto.MaxUsers,
                IsPublic = dto.IsPublic,
                IsActive = dto.IsActive,
                DisplayOrder = dto.DisplayOrder
            };

            _context.Plans.Add(plan);
            await _context.SaveChangesAsync();

            return OperationResult<PlanAdminDto>.Ok(
                new PlanAdminDto(plan.Id, plan.Code, plan.Name, plan.Description,
                    plan.MonthlyPrice, plan.AnnualPrice, plan.MaxBranches, plan.MaxUsers,
                    plan.IsPublic, plan.IsActive, plan.DisplayOrder, 0));
        }

        // ── UPDATE ────────────────────────────────────────────────────
        public async Task<OperationResult<PlanAdminDto>> Update(int planId, PlanUpsertDto dto)
        {
            var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == planId);
            if (plan is null)
                return OperationResult<PlanAdminDto>.NotFound(
                    "Plan no encontrado.",
                    ErrorKeys.PlanNotFound);

            var code = dto.Code.Trim().ToUpper();
            if (code != plan.Code &&
                await _context.Plans.AnyAsync(p => p.Code == code && p.Id != planId))
                return OperationResult<PlanAdminDto>.Conflict(
                    $"Ya existe un plan con el código '{code}'.",
                    ErrorKeys.PlanCodeAlreadyExists);

            plan.Code = code;
            plan.Name = dto.Name.Trim();
            plan.Description = dto.Description?.Trim();
            plan.MonthlyPrice = dto.MonthlyPrice;
            plan.AnnualPrice = dto.AnnualPrice;
            plan.MaxBranches = dto.MaxBranches;
            plan.MaxUsers = dto.MaxUsers;
            plan.IsPublic = dto.IsPublic;
            plan.IsActive = dto.IsActive;
            plan.DisplayOrder = dto.DisplayOrder;

            await _context.SaveChangesAsync();

            var tenantCount = await _context.Companies.CountAsync(c => c.PlanId == planId);
            return OperationResult<PlanAdminDto>.Ok(
                new PlanAdminDto(plan.Id, plan.Code, plan.Name, plan.Description,
                    plan.MonthlyPrice, plan.AnnualPrice, plan.MaxBranches, plan.MaxUsers,
                    plan.IsPublic, plan.IsActive, plan.DisplayOrder, tenantCount));
        }

        // ── TOGGLE ACTIVE ─────────────────────────────────────────────
        public async Task<OperationResult<bool>> ToggleActive(int planId)
        {
            var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == planId);
            if (plan is null)
                return OperationResult<bool>.NotFound(
                    "Plan no encontrado.",
                    ErrorKeys.PlanNotFound);

            plan.IsActive = !plan.IsActive;
            await _context.SaveChangesAsync();
            return OperationResult<bool>.Ok(plan.IsActive);
        }
    }
}
