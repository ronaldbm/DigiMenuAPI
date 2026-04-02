using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Services;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.UnitTests.TestInfrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.UnitTests.Application.Services;

/// <summary>
/// Tests de CustomerService — gestión de clientes por Company.
///
/// Aspectos críticos:
///   1. Multi-tenant: ValidateCompanyOwnership lanza excepción si el cliente no pertenece a la company.
///   2. GetAll filtra solo clientes de la company del tenant.
///   3. GetAll con búsqueda filtra por name/phone/email.
///   4. Create asigna CompanyId del tenant automáticamente.
///   5. ToggleActive invierte IsActive.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "TenantIsolation")]
public sealed class CustomerServiceTests : ServiceTestBase
{
    private CustomerService CreateService()
        => new(Db, TenantService);

    // ── Helpers locales ───────────────────────────────────────────────────

    private async Task<Customer> SeedCustomerAsync(
        int id        = 100,
        int companyId = 100,
        string name   = "Cliente Test",
        bool isActive = true)
    {
        var customer = new Customer
        {
            Id          = id,
            CompanyId   = companyId,
            Name        = name,
            IsActive    = isActive,
            CreditLimit = 0m,
            MaxOpenTabs = 1,
            MaxTabAmount = 0m,
        };
        Db.Customers.Add(customer);
        await Db.SaveChangesAsync();
        return customer;
    }

    // ── 1. GetAll ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOnlyCurrentTenantCustomers()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedCustomerAsync(id: 100, companyId: 100, name: "Ana");
        await SeedCustomerAsync(id: 101, companyId: 100, name: "Carlos");
        await SeedCustomerAsync(id: 102, companyId: 101, name: "Otro Tenant");

        SetTenant(companyId: 100);
        var result = await CreateService().GetAll(null, 1, 10);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
        result.Data.Items.Should().NotContain(c => c.Name == "Otro Tenant");
    }

    [Fact]
    public async Task GetAll_WithSearch_FiltersbyName()
    {
        await SeedCompanyAsync(100);
        await SeedCustomerAsync(id: 100, companyId: 100, name: "Ana García");
        await SeedCustomerAsync(id: 101, companyId: 100, name: "Carlos López");

        SetTenant(companyId: 100);
        var result = await CreateService().GetAll("ana", 1, 10);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items.First().Name.Should().Be("Ana García");
    }

    [Fact]
    public async Task GetAll_Pagination_ReturnsCorrectPage()
    {
        await SeedCompanyAsync(100);
        for (int i = 1; i <= 5; i++)
            await SeedCustomerAsync(id: 100 + i, companyId: 100, name: $"Cliente {i}");

        SetTenant(companyId: 100);
        var result = await CreateService().GetAll(null, page: 2, pageSize: 2);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
        result.Data.Total.Should().Be(5);
        result.Data.Page.Should().Be(2);
    }

    // ── 2. GetById ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_NotFound_ReturnsNotFound()
    {
        SetTenant(companyId: 100);
        var result = await CreateService().GetById(9999);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.CustomerNotFound);
    }

    [Fact]
    public async Task GetById_BelongsToOtherTenant_Throws()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedCustomerAsync(id: 100, companyId: 101);

        SetTenant(companyId: 100);
        Func<Task> act = () => CreateService().GetById(100);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetById_ValidCustomer_ReturnsDetail()
    {
        await SeedCompanyAsync(100);
        await SeedCustomerAsync(id: 100, companyId: 100, name: "María González");

        SetTenant(companyId: 100);
        var result = await CreateService().GetById(100);

        result.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("María González");
    }

    // ── 3. Create ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidCustomer_AssignsTenantCompanyId()
    {
        await SeedCompanyAsync(100);
        SetTenant(companyId: 100);

        var result = await CreateService().Create(new CustomerCreateDto
        {
            Name         = "Nuevo Cliente",
            Phone        = "+50612345678",
            CreditLimit  = 50000m,
            MaxOpenTabs  = 2,
            MaxTabAmount = 25000m,
        });

        result.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("Nuevo Cliente");

        var saved = await Db.Customers.FirstAsync(c => c.Name == "Nuevo Cliente");
        saved.CompanyId.Should().Be(100);
        saved.IsActive.Should().BeTrue();
    }

    // ── 4. Update ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_NotFound_ReturnsNotFound()
    {
        SetTenant(companyId: 100);
        var result = await CreateService().Update(new CustomerUpdateDto
        {
            Id           = 9999,
            Name         = "X",
            CreditLimit  = 0m,
            MaxOpenTabs  = 1,
            MaxTabAmount = 0m,
            IsActive     = true,
        });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
    }

    [Fact]
    public async Task Update_ValidData_UpdatesFields()
    {
        await SeedCompanyAsync(100);
        await SeedCustomerAsync(id: 100, companyId: 100, name: "Original");

        SetTenant(companyId: 100);
        var result = await CreateService().Update(new CustomerUpdateDto
        {
            Id           = 100,
            Name         = "Actualizado",
            Phone        = "+50699999999",
            CreditLimit  = 100000m,
            MaxOpenTabs  = 3,
            MaxTabAmount = 50000m,
            IsActive     = true,
        });

        result.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("Actualizado");

        var updated = await Db.Customers.FindAsync(100);
        updated!.CreditLimit.Should().Be(100000m);
    }

    // ── 5. ToggleActive ───────────────────────────────────────────────────

    [Fact]
    public async Task ToggleActive_ActiveCustomer_BecomesInactive()
    {
        await SeedCompanyAsync(100);
        await SeedCustomerAsync(id: 100, companyId: 100, isActive: true);

        SetTenant(companyId: 100);
        var result = await CreateService().ToggleActive(100);

        result.Success.Should().BeTrue();

        var updated = await Db.Customers.FindAsync(100);
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
}
