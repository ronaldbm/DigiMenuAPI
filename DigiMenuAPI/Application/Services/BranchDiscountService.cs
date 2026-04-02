using AppCore.Application.Common;
using AppCore.Application.Interfaces;
using DigiMenuAPI.Application.Common.Enums;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services;

public class BranchDiscountService : IBranchDiscountService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantService       _tenantService;

    public BranchDiscountService(
        ApplicationDbContext context,
        ITenantService       tenantService)
    {
        _context       = context;
        _tenantService = tenantService;
    }

    public async Task<OperationResult<List<BranchDiscountReadDto>>> GetByBranch(int branchId)
    {
        await _tenantService.ValidateBranchOwnershipAsync(branchId);

        var discounts = await _context.BranchDiscounts
            .AsNoTracking()
            .Where(d => d.BranchId == branchId)
            .OrderBy(d => d.Name)
            .ToListAsync();

        return OperationResult<List<BranchDiscountReadDto>>.Ok(
            discounts.Select(MapToDto).ToList());
    }

    public async Task<OperationResult<BranchDiscountReadDto>> GetById(int id)
    {
        var discount = await _context.BranchDiscounts
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        if (discount is null)
            return OperationResult<BranchDiscountReadDto>.NotFound(
                "Descuento no encontrado.", ErrorKeys.BranchDiscountNotFound);

        await _tenantService.ValidateBranchOwnershipAsync(discount.BranchId);
        return OperationResult<BranchDiscountReadDto>.Ok(MapToDto(discount));
    }

    public async Task<OperationResult<BranchDiscountReadDto>> Create(BranchDiscountCreateDto dto)
    {
        await _tenantService.ValidateBranchOwnershipAsync(dto.BranchId);

        var discount = new BranchDiscount
        {
            BranchId         = dto.BranchId,
            Name             = dto.Name.Trim(),
            DiscountType     = dto.DiscountType,
            DefaultValue     = dto.DefaultValue,
            AppliesTo        = dto.AppliesTo,
            RequiresApproval = dto.RequiresApproval,
            MaxValueForStaff = dto.MaxValueForStaff,
            IsActive         = true,
        };

        _context.BranchDiscounts.Add(discount);
        await _context.SaveChangesAsync();

        return OperationResult<BranchDiscountReadDto>.Ok(
            MapToDto(discount), "Descuento creado correctamente.");
    }

    public async Task<OperationResult<BranchDiscountReadDto>> Update(BranchDiscountUpdateDto dto)
    {
        var discount = await _context.BranchDiscounts
            .FirstOrDefaultAsync(d => d.Id == dto.Id);

        if (discount is null)
            return OperationResult<BranchDiscountReadDto>.NotFound(
                "Descuento no encontrado.", ErrorKeys.BranchDiscountNotFound);

        await _tenantService.ValidateBranchOwnershipAsync(discount.BranchId);

        discount.Name             = dto.Name.Trim();
        discount.DiscountType     = dto.DiscountType;
        discount.DefaultValue     = dto.DefaultValue;
        discount.AppliesTo        = dto.AppliesTo;
        discount.RequiresApproval = dto.RequiresApproval;
        discount.MaxValueForStaff = dto.MaxValueForStaff;
        discount.IsActive         = dto.IsActive;

        await _context.SaveChangesAsync();

        return OperationResult<BranchDiscountReadDto>.Ok(
            MapToDto(discount), "Descuento actualizado correctamente.");
    }

    public async Task<OperationResult<bool>> ToggleActive(int id)
    {
        var discount = await _context.BranchDiscounts
            .FirstOrDefaultAsync(d => d.Id == id);

        if (discount is null)
            return OperationResult<bool>.NotFound(
                "Descuento no encontrado.", ErrorKeys.BranchDiscountNotFound);

        await _tenantService.ValidateBranchOwnershipAsync(discount.BranchId);

        discount.IsActive = !discount.IsActive;
        await _context.SaveChangesAsync();

        return OperationResult<bool>.Ok(discount.IsActive,
            discount.IsActive ? "Descuento activado." : "Descuento desactivado.");
    }

    public async Task<OperationResult<bool>> Delete(int id)
    {
        var discount = await _context.BranchDiscounts
            .FirstOrDefaultAsync(d => d.Id == id);

        if (discount is null)
            return OperationResult<bool>.NotFound(
                "Descuento no encontrado.", ErrorKeys.BranchDiscountNotFound);

        await _tenantService.ValidateBranchOwnershipAsync(discount.BranchId);

        // Cannot delete if there are active references
        var inUse = await _context.AccountDiscounts
            .AnyAsync(ad => ad.BranchDiscountId == id
                         && (ad.Status == AccountDiscountStatus.Approved
                          || ad.Status == AccountDiscountStatus.PendingApproval));

        if (inUse)
            return OperationResult<bool>.Conflict(
                "No se puede eliminar un descuento que está en uso en cuentas activas.",
                ErrorKeys.BranchDiscountInUse);

        _context.BranchDiscounts.Remove(discount);
        await _context.SaveChangesAsync();

        return OperationResult<bool>.Ok(true, "Descuento eliminado correctamente.");
    }

    private static BranchDiscountReadDto MapToDto(BranchDiscount d) =>
        new(
            Id:               d.Id,
            BranchId:         d.BranchId,
            Name:             d.Name,
            DiscountType:     d.DiscountType,
            DiscountTypeName: d.DiscountType == DiscountType.Percentage ? "Porcentaje" : "Monto fijo",
            DefaultValue:     d.DefaultValue,
            AppliesTo:        d.AppliesTo,
            AppliesToName:    d.AppliesTo switch
            {
                DiscountAppliesTo.WholeAccount => "Cuenta completa",
                DiscountAppliesTo.SpecificItem => "Ítem específico",
                DiscountAppliesTo.Both         => "Ambos",
                _                              => d.AppliesTo.ToString()
            },
            RequiresApproval: d.RequiresApproval,
            MaxValueForStaff: d.MaxValueForStaff,
            IsActive:         d.IsActive,
            CreatedAt:        d.CreatedAt
        );
}
