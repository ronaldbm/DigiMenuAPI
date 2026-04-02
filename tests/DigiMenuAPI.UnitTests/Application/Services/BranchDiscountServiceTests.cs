using AppCore.Application.Common;
using DigiMenuAPI.Application.Common.Enums;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Services;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.UnitTests.TestInfrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.UnitTests.Application.Services;

/// <summary>
/// Tests de BranchDiscountService — gestión de descuentos por Branch.
///
/// Aspectos críticos:
///   1. Multi-tenant: ValidateBranchOwnershipAsync lanza excepción si la branch no pertenece al tenant.
///   2. Delete bloqueado si el descuento está en uso (estado Approved o PendingApproval).
///   3. ToggleActive alterna IsActive.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "TenantIsolation")]
public sealed class BranchDiscountServiceTests : ServiceTestBase
{
    private BranchDiscountService CreateService()
        => new(Db, TenantService);

    // ── Helpers locales ───────────────────────────────────────────────────

    private async Task<BranchDiscount> SeedDiscountAsync(
        int id       = 100,
        int branchId = 100,
        string name  = "Descuento Test",
        bool isActive = true)
    {
        var discount = new BranchDiscount
        {
            Id               = id,
            BranchId         = branchId,
            Name             = name,
            DiscountType     = DiscountType.Percentage,
            DefaultValue     = 10m,
            AppliesTo        = DiscountAppliesTo.WholeAccount,
            RequiresApproval = false,
            IsActive         = isActive,
        };
        Db.BranchDiscounts.Add(discount);
        await Db.SaveChangesAsync();
        return discount;
    }

    // ── 1. GetByBranch ────────────────────────────────────────────────────

    [Fact]
    public async Task GetByBranch_ValidBranch_ReturnsDiscounts()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedDiscountAsync(id: 100, branchId: 100, name: "Descuento A");
        await SeedDiscountAsync(id: 101, branchId: 100, name: "Descuento B");

        SetTenant(companyId: 100);
        var result = await CreateService().GetByBranch(100);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByBranch_BranchBelongsToOtherTenant_Throws()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedBranchAsync(id: 110, companyId: 101);

        SetTenant(companyId: 100);
        Func<Task> act = () => CreateService().GetByBranch(110);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ── 2. GetById ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_NotFound_ReturnsNotFound()
    {
        SetTenant(companyId: 100);
        var result = await CreateService().GetById(9999);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.BranchDiscountNotFound);
    }

    [Fact]
    public async Task GetById_ValidDiscount_ReturnsDto()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedDiscountAsync(id: 100, branchId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().GetById(100);

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(100);
        result.Data.BranchId.Should().Be(100);
    }

    // ── 3. Create ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidDiscount_PersistsWithIsActiveTrue()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().Create(new BranchDiscountCreateDto
        {
            BranchId         = 100,
            Name             = "Happy Hour 20%",
            DiscountType     = DiscountType.Percentage,
            DefaultValue     = 20m,
            AppliesTo        = DiscountAppliesTo.WholeAccount,
            RequiresApproval = false,
        });

        result.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("Happy Hour 20%");
        result.Data.IsActive.Should().BeTrue();

        var saved = await Db.BranchDiscounts.FirstAsync(d => d.Name == "Happy Hour 20%");
        saved.DefaultValue.Should().Be(20m);
    }

    [Fact]
    public async Task Create_BranchBelongsToOtherTenant_Throws()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedBranchAsync(id: 110, companyId: 101);

        SetTenant(companyId: 100);
        Func<Task> act = () => CreateService().Create(new BranchDiscountCreateDto
        {
            BranchId     = 110,
            Name         = "Hack",
            DiscountType = DiscountType.Percentage,
            DefaultValue = 10m,
            AppliesTo    = DiscountAppliesTo.WholeAccount,
        });

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ── 4. Update ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_NotFound_ReturnsNotFound()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        SetTenant(companyId: 100);

        var result = await CreateService().Update(new BranchDiscountUpdateDto
        {
            Id           = 9999,
            BranchId     = 100,
            Name         = "X",
            DiscountType = DiscountType.Percentage,
            DefaultValue = 10m,
            AppliesTo    = DiscountAppliesTo.WholeAccount,
            IsActive     = true,
        });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.BranchDiscountNotFound);
    }

    [Fact]
    public async Task Update_ValidData_UpdatesFields()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedDiscountAsync(id: 100, branchId: 100, name: "Original");

        SetTenant(companyId: 100);
        var result = await CreateService().Update(new BranchDiscountUpdateDto
        {
            Id           = 100,
            BranchId     = 100,
            Name         = "Actualizado",
            DiscountType = DiscountType.FixedAmount,
            DefaultValue = 500m,
            AppliesTo    = DiscountAppliesTo.SpecificItem,
            IsActive     = true,
        });

        result.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("Actualizado");
        result.Data.DefaultValue.Should().Be(500m);

        var updated = await Db.BranchDiscounts.FindAsync(100);
        updated!.DiscountType.Should().Be(DiscountType.FixedAmount);
    }

    // ── 5. ToggleActive ───────────────────────────────────────────────────

    [Fact]
    public async Task ToggleActive_ActiveDiscount_BecomesInactive()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedDiscountAsync(id: 100, branchId: 100, isActive: true);

        SetTenant(companyId: 100);
        var result = await CreateService().ToggleActive(100);

        result.Success.Should().BeTrue();
        result.Data.Should().BeFalse(); // ahora inactivo

        var updated = await Db.BranchDiscounts.FindAsync(100);
        updated!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleActive_NotFound_ReturnsNotFound()
    {
        SetTenant(companyId: 100);
        var result = await CreateService().ToggleActive(9999);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
    }

    // ── 6. Delete ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_NotFound_ReturnsNotFound()
    {
        SetTenant(companyId: 100);
        var result = await CreateService().Delete(9999);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.BranchDiscountNotFound);
    }

    [Fact]
    public async Task Delete_DiscountInUse_ReturnsConflict()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedDiscountAsync(id: 100, branchId: 100);

        // Crear AccountDiscount activo que bloquea la eliminación
        Db.AccountDiscounts.Add(new AccountDiscount
        {
            Id               = 100,
            BranchDiscountId = 100,
            AccountId        = 100, // FK no validada en InMemory
            Reason           = "Test",
            DiscountType     = DiscountType.Percentage,
            DiscountValue    = 10m,
            AppliesTo        = DiscountAppliesTo.WholeAccount,
            Status           = AccountDiscountStatus.Approved,
        });
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var result = await CreateService().Delete(100);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Conflict);
        result.ErrorKey.Should().Be(ErrorKeys.BranchDiscountInUse);
    }

    [Fact]
    public async Task Delete_ValidDiscount_RemovesFromDb()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedDiscountAsync(id: 100, branchId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().Delete(100);

        result.Success.Should().BeTrue();

        var deleted = await Db.BranchDiscounts.FindAsync(100);
        deleted.Should().BeNull();
    }
}
