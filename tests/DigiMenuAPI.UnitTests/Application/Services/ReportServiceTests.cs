using AppCore.Application.Common;
using DigiMenuAPI.Application.Common.Enums;
using DigiMenuAPI.Application.Services;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.UnitTests.TestInfrastructure;
using FluentAssertions;

namespace DigiMenuAPI.UnitTests.Application.Services;

/// <summary>
/// Tests de ReportService — KPIs de cuentas y reportes.
///
/// Aspectos críticos:
///   1. Multi-tenant: solo cuenta cuentas de la company del tenant.
///   2. GetAccountKpis: KPIs correctos con cuentas Open, PendingPayment, Closed.
///   3. GetCustomerStatement: NotFound si el cliente no existe o no pertenece a la company.
///   4. GetAccountReceipt: NotFound si la cuenta no existe.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "TenantIsolation")]
public sealed class ReportServiceTests : ServiceTestBase
{
    private ReportService CreateService()
        => new(Db, TenantService);

    // ── Helpers locales ───────────────────────────────────────────────────

    private async Task<Account> SeedAccountAsync(
        int id       = 100,
        int branchId = 100,
        AccountStatus status = AccountStatus.Open,
        string clientId = "Mesa 1")
    {
        var account = new Account
        {
            Id               = id,
            BranchId         = branchId,
            ClientIdentifier = clientId,
            Status           = status,
        };
        Db.Accounts.Add(account);
        await Db.SaveChangesAsync();
        return account;
    }

    // ── 1. GetAccountKpis ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAccountKpis_EmptyDatabase_ReturnsAllZeros()
    {
        await SeedCompanyAsync(100);
        SetTenant(companyId: 100);

        var result = await CreateService().GetAccountKpis(null, null, null);

        result.Success.Should().BeTrue();
        result.Data!.OpenAccounts.Should().Be(0);
        result.Data.TabAccounts.Should().Be(0);
        result.Data.RevenueToday.Should().Be(0m);
        result.Data.PendingDiscounts.Should().Be(0);
    }

    [Fact]
    public async Task GetAccountKpis_CountsOpenAndTabAccounts()
    {
        await SeedCompanyAsync(100);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedAccountAsync(id: 100, branchId: 100, status: AccountStatus.Open,   clientId: "Mesa 1");
        await SeedAccountAsync(id: 101, branchId: 100, status: AccountStatus.Open,   clientId: "Mesa 2");
        await SeedAccountAsync(id: 102, branchId: 100, status: AccountStatus.PendingPayment, clientId: "Tab A");
        await SeedAccountAsync(id: 103, branchId: 100, status: AccountStatus.Closed, clientId: "Mesa 3");

        SetTenant(companyId: 100);
        var result = await CreateService().GetAccountKpis(null, null, null);

        result.Success.Should().BeTrue();
        result.Data!.OpenAccounts.Should().Be(2);  // 2 Open
        result.Data.TabAccounts.Should().Be(1);    // 1 PendingPayment
    }

    [Fact]
    public async Task GetAccountKpis_OnlyCurrentTenantAccounts()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedBranchAsync(id: 100, companyId: 100);
        await SeedBranchAsync(id: 101, companyId: 101);

        await SeedAccountAsync(id: 100, branchId: 100, status: AccountStatus.Open);
        await SeedAccountAsync(id: 101, branchId: 101, status: AccountStatus.Open); // otro tenant

        SetTenant(companyId: 100);
        var result = await CreateService().GetAccountKpis(null, null, null);

        result.Success.Should().BeTrue();
        result.Data!.OpenAccounts.Should().Be(1); // solo la cuenta del tenant 100
    }

    // ── 2. GetCustomerStatement ───────────────────────────────────────────

    [Fact]
    public async Task GetCustomerStatement_CustomerNotFound_ReturnsNotFound()
    {
        SetTenant(companyId: 100);
        var result = await CreateService().GetCustomerStatement(9999, null, null);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.CustomerNotFound);
    }

    [Fact]
    public async Task GetCustomerStatement_CustomerBelongsToOtherTenant_ReturnsNotFound()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);

        // Customer de tenant 101
        var customer = new Customer
        {
            Id           = 100,
            CompanyId    = 101,
            Name         = "Cliente Otro Tenant",
            CreditLimit  = 0m,
            MaxOpenTabs  = 1,
            MaxTabAmount = 0m,
        };
        Db.Customers.Add(customer);
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100); // tenant 100 no debe ver el cliente
        var result = await CreateService().GetCustomerStatement(100, null, null);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
    }

    [Fact]
    public async Task GetCustomerStatement_ValidCustomer_ReturnsStatement()
    {
        await SeedCompanyAsync(100);

        var customer = new Customer
        {
            Id           = 100,
            CompanyId    = 100,
            Name         = "María López",
            CreditLimit  = 50000m,
            MaxOpenTabs  = 2,
            MaxTabAmount = 25000m,
        };
        Db.Customers.Add(customer);
        await Db.SaveChangesAsync();

        SetTenant(companyId: 100);
        var result = await CreateService().GetCustomerStatement(100, null, null);

        result.Success.Should().BeTrue();
        result.Data!.CustomerName.Should().Be("María López");
        result.Data.CreditLimit.Should().Be(50000m);
        result.Data.Lines.Should().BeEmpty(); // sin cuentas en el período
    }

    // ── 3. GetAccountReceipt ──────────────────────────────────────────────

    [Fact]
    public async Task GetAccountReceipt_NotFound_ReturnsNotFound()
    {
        SetTenant(companyId: 100);
        var result = await CreateService().GetAccountReceipt(9999);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
        result.ErrorKey.Should().Be(ErrorKeys.AccountNotFound);
    }

    [Fact]
    public async Task GetAccountReceipt_AccountBelongsToOtherTenant_ReturnsNotFound()
    {
        await SeedCompanyAsync(100);
        await SeedCompanyAsync(101);
        await SeedBranchAsync(id: 101, companyId: 101);
        await SeedAccountAsync(id: 100, branchId: 101); // branch pertenece a tenant 101

        SetTenant(companyId: 100); // tenant 100 no puede ver esa cuenta
        var result = await CreateService().GetAccountReceipt(100);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(OperationResultError.NotFound);
    }

    // ── 4. GetAccountReport ───────────────────────────────────────────────

    [Fact]
    public async Task GetAccountReport_EmptyDatabase_ReturnsEmptyPage()
    {
        await SeedCompanyAsync(100);
        SetTenant(companyId: 100);

        var result = await CreateService().GetAccountReport(
            null, null, null, null, null, false, 1, 10);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        result.Data.Total.Should().Be(0);
    }
}
