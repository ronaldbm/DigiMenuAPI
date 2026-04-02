using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Services;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.UnitTests.TestInfrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace DigiMenuAPI.UnitTests.Application.Services;

/// <summary>
/// Tests de FooterLinkService — gestión de enlaces del footer por Branch.
///
/// Aspectos críticos:
///   1. Multi-tenant: ValidateBranchOwnershipAsync lanza excepción si la branch no pertenece al tenant.
///   2. BranchAdmin solo puede editar/borrar sus propios enlaces.
///   3. Soft-delete via IsDeleted + cache eviction.
///
/// Seed: FooterLinks IDs 1-2 para Branch 1 → usar IDs >= 100 en tests.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "TenantIsolation")]
public sealed class FooterLinkServiceTests : ServiceTestBase
{
    private FooterLinkService CreateService()
        => new(Db, Mapper, TenantService, CacheService);

    // ── Helpers locales ───────────────────────────────────────────────────

    private async Task<FooterLink> SeedFooterLinkAsync(
        int id       = 100,
        int branchId = 100,
        string label = "Instagram",
        int displayOrder = 1)
    {
        var link = new FooterLink
        {
            Id           = id,
            BranchId     = branchId,
            Label        = label,
            Url          = "https://instagram.com/test",
            DisplayOrder = displayOrder,
            IsVisible    = true,
            IsDeleted    = false,
        };
        Db.FooterLinks.Add(link);
        await Db.SaveChangesAsync();
        return link;
    }

    // ── 1. GetAll ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ValidBranch_ReturnsLinks()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedFooterLinkAsync(id: 100, branchId: 100, label: "Instagram");
        await SeedFooterLinkAsync(id: 101, branchId: 100, label: "WhatsApp");

        SetTenant(companyId: 100);
        var result = await CreateService().GetAll(100);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_ExcludesSoftDeletedLinks()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedFooterLinkAsync(id: 100, branchId: 100);
        var deleted = await SeedFooterLinkAsync(id: 101, branchId: 100, label: "Deleted");
        deleted.IsDeleted = true;
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var result = await CreateService().GetAll(100);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data!.First().Id.Should().Be(100);
    }

    [Fact]
    public async Task GetAll_BranchBelongsToOtherTenant_Throws()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedBranchAsync(id: 110, companyId: 101);

        SetTenant(companyId: 100);
        Func<Task> act = () => CreateService().GetAll(110);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ── 2. Create ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidLink_PersistsAndEvictsCache()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().Create(new FooterLinkCreateDto(
            BranchId:        100,
            Label:           "Facebook",
            Url:             "https://facebook.com/test",
            StandardIconId:  null,
            CustomSvgContent: null,
            DisplayOrder:    1,
            IsVisible:       true));

        result.Success.Should().BeTrue();
        result.Data!.Label.Should().Be("Facebook");

        var saved = await Db.FooterLinks.FirstAsync(f => f.Label == "Facebook");
        saved.BranchId.Should().Be(100);

        await CacheService.Received(1).EvictMenuByBranchAsync(100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_BranchBelongsToOtherTenant_Throws()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedBranchAsync(id: 110, companyId: 101);

        SetTenant(companyId: 100);
        Func<Task> act = () => CreateService().Create(new FooterLinkCreateDto(
            BranchId: 110, Label: "Hack", Url: "https://hack.com",
            StandardIconId: null, CustomSvgContent: null,
            DisplayOrder: 1, IsVisible: true));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ── 3. Update ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_LinkBelongsToOtherTenant_ReturnsNotFound()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedBranchAsync(id: 110, companyId: 101);
        await SeedFooterLinkAsync(id: 110, branchId: 110);

        SetTenant(companyId: 100);
        var result = await CreateService().Update(new FooterLinkUpdateDto(
            Id: 110, Label: "Hack", Url: "https://hack.com",
            StandardIconId: null, CustomSvgContent: null,
            DisplayOrder: 1, IsVisible: true));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.FooterLinkNotFound);
    }

    [Fact]
    public async Task Update_ValidLink_UpdatesAndEvictsCache()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedFooterLinkAsync(id: 100, branchId: 100, label: "Original");

        SetTenant(companyId: 100);
        var result = await CreateService().Update(new FooterLinkUpdateDto(
            Id: 100, Label: "Actualizado", Url: "https://updated.com",
            StandardIconId: null, CustomSvgContent: null,
            DisplayOrder: 2, IsVisible: true));

        result.Success.Should().BeTrue();

        var updated = await Db.FooterLinks.FindAsync(100);
        updated!.Label.Should().Be("Actualizado");

        await CacheService.Received(1).EvictMenuByBranchAsync(100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_BranchAdminTriesToUpdateOtherBranchLink_ReturnsForbidden()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedBranchAsync(id: 101, companyId: 100);
        await SeedFooterLinkAsync(id: 100, branchId: 101); // link en branch 101

        // BranchAdmin de branch 100 intenta editar link de branch 101
        SetTenant(companyId: 100, branchId: 100, role: UserRoles.BranchAdmin);
        var result = await CreateService().Update(new FooterLinkUpdateDto(
            Id: 100, Label: "Hack", Url: "https://hack.com",
            StandardIconId: null, CustomSvgContent: null,
            DisplayOrder: 1, IsVisible: true));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Forbidden);
        result.ErrorKey.Should().Be(ErrorKeys.FooterLinkNotOwned);
    }

    // ── 4. Delete ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_NotFound_ReturnsNotFound()
    {
        SetTenant(companyId: 100);
        var result = await CreateService().Delete(9999);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.FooterLinkNotFound);
    }

    [Fact]
    public async Task Delete_ValidLink_SoftDeletesAndEvictsCache()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedFooterLinkAsync(id: 100, branchId: 100);

        SetTenant(companyId: 100);
        var result = await CreateService().Delete(100);

        result.Success.Should().BeTrue();

        var deleted = await Db.FooterLinks.IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.Id == 100);
        deleted!.IsDeleted.Should().BeTrue();

        await CacheService.Received(1).EvictMenuByBranchAsync(100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_BranchAdminTriesToDeleteOtherBranchLink_ReturnsForbidden()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedBranchAsync(id: 101, companyId: 100);
        await SeedFooterLinkAsync(id: 100, branchId: 101); // link en branch 101

        // BranchAdmin de branch 100 intenta borrar link de branch 101
        SetTenant(companyId: 100, branchId: 100, role: UserRoles.BranchAdmin);
        var result = await CreateService().Delete(100);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.Forbidden);
    }
}
