using AppCore.Application.Common;
using AppCore.Application.Interfaces;
using AppCore.Domain.Entities;
using AppCore.UnitTests.TestInfrastructure;
using AutoMapper;
using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace DigiMenuAPI.UnitTests.TestInfrastructure;

/// <summary>
/// Clase base para todos los tests de servicios.
///
/// Provee:
///   - ApplicationDbContext con InMemory aislado por test (factory única).
///   - FakeTenantService configurable por rol/company/branch.
///   - AutoMapper real con los perfiles de producción.
///   - ICacheService stub (no-op, para no interferir en tests).
///   - IModuleGuard stub (permite todo por defecto).
///   - Métodos helper de seed para entidades comunes.
/// </summary>
public abstract class ServiceTestBase : IDisposable
{
    protected readonly TestApplicationDbContextFactory DbFactory;
    protected ApplicationDbContext Db => DbFactory.Context;

    protected readonly IMapper Mapper;
    protected readonly ICacheService CacheService;
    protected readonly IModuleGuard ModuleGuard;
    protected readonly IEmailQueueService EmailQueue;
    protected readonly IFileStorageService FileStorage;
    protected readonly IConfiguration Configuration;

    // TenantService por defecto: CompanyAdmin, company=1, sin branch
    protected FakeTenantService TenantService { get; private set; }

    protected ServiceTestBase()
    {
        DbFactory = new TestApplicationDbContextFactory();

        // AutoMapper 16.x requiere ILoggerFactory como segundo parámetro
        var mapperConfig = new MapperConfiguration(
            cfg => cfg.AddProfile<AutoMapperProfiles>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        Mapper = mapperConfig.CreateMapper();

        // Stubs no-op
        CacheService = Substitute.For<ICacheService>();
        ModuleGuard  = Substitute.For<IModuleGuard>();
        EmailQueue   = Substitute.For<IEmailQueueService>();
        FileStorage  = Substitute.For<IFileStorageService>();

        // IConfiguration mínima para AuthService, UserService, etc.
        Configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:AppUrl"]         = "https://test.digimenu.cr",
                ["Jwt:Key"]              = "test-secret-key-must-be-at-least-32-characters!",
                ["Jwt:Issuer"]           = "test-issuer",
                ["Jwt:Audience"]         = "test-audience",
                ["Jwt:ExpirationHours"]  = "8",
            })
            .Build();

        // Tenant por defecto: CompanyAdmin, companyId=1
        TenantService = CreateTenantService();
    }

    // ── Configuración de contexto de tenant ──────────────────────────────

    /// <summary>Crea un FakeTenantService con los parámetros dados.</summary>
    protected FakeTenantService CreateTenantService(
        int companyId = 1,
        int? branchId = null,
        int userId = 1,
        byte role = UserRoles.CompanyAdmin)
        => new(companyId, branchId, userId, role, Db);

    /// <summary>Cambia el tenant activo para el test en curso.</summary>
    protected void SetTenant(
        int companyId = 1,
        int? branchId = null,
        int userId = 1,
        byte role = UserRoles.CompanyAdmin)
        => TenantService = CreateTenantService(companyId, branchId, userId, role);

    // ── Seed helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Siembra una Company de test. Usa IDs >= 100 para no colisionar con la
    /// company maestra (Id=1 "digimenu-platform") seedeada por SeedCoreData.
    /// Si la company ya existe (por el seed del contexto) la retorna directamente.
    /// </summary>
    protected async Task<Company> SeedCompanyAsync(int id = 100, string slug = "test-company")
    {
        var existing = await Db.Companies.FindAsync(id);
        if (existing is not null) return existing;

        await SeedPlanAsync();
        var company = new Company
        {
            Id          = id,
            Name        = $"Test Company {id}",
            Slug        = $"{slug}-{id}",
            Email       = $"company{id}@test.com",
            IsActive    = true,
            PlanId      = 99,   // plan de test, no conflicta con los seedeados (1-4)
            MaxBranches = -1,
            MaxUsers    = -1,
        };
        Db.Companies.Add(company);
        await Db.SaveChangesAsync();
        return company;
    }

    protected async Task<Plan> SeedPlanAsync(int id = 99, string code = "TEST_PLAN_99")
    {
        var existing = await Db.Plans.FindAsync(id);
        if (existing is not null) return existing;

        var plan = new Plan
        {
            Id           = id,
            Code         = code,
            Name         = "Test Plan",
            MonthlyPrice = 0,
            AnnualPrice  = 0,
            MaxBranches  = -1,
            MaxUsers     = -1,
        };
        Db.Plans.Add(plan);
        await Db.SaveChangesAsync();
        return plan;
    }

    protected async Task<Branch> SeedBranchAsync(
        int id = 1,
        int companyId = 1,
        string slug = "main-branch",
        bool isActive = true)
    {
        var branch = new Branch
        {
            Id        = id,
            CompanyId = companyId,
            Name      = $"Branch {id}",
            Slug      = slug,
            IsActive  = isActive,
            IsDeleted = false,
        };
        Db.Branches.Add(branch);
        await Db.SaveChangesAsync();
        return branch;
    }

    protected async Task<AppUser> SeedUserAsync(
        int id = 1,
        int companyId = 1,
        int? branchId = null,
        byte role = UserRoles.CompanyAdmin)
    {
        var user = new AppUser
        {
            Id           = id,
            CompanyId    = companyId,
            BranchId     = branchId,
            FullName     = $"Test User {id}",
            Email        = $"user{id}@test.com",
            PasswordHash = "$2a$11$test_hash_placeholder",
            Role         = role,
            IsActive     = true,
            IsDeleted    = false,
        };
        Db.Users.Add(user);
        await Db.SaveChangesAsync();
        return user;
    }

    protected async Task<Category> SeedCategoryAsync(
        int id = 1,
        int companyId = 1,
        int displayOrder = 1,
        bool isDeleted = false,
        string translationName = "Test Category",
        string langCode = "es")
    {
        var category = new Category
        {
            Id           = id,
            CompanyId    = companyId,
            DisplayOrder = displayOrder,
            IsVisible    = true,
            IsDeleted    = isDeleted,
            Translations =
            [
                new CategoryTranslation
                {
                    LanguageCode = langCode,
                    Name         = translationName,
                }
            ],
        };
        Db.Categories.Add(category);
        await Db.SaveChangesAsync();
        return category;
    }

    protected async Task<Product> SeedProductAsync(
        int id = 1,
        int companyId = 1,
        int categoryId = 1,
        string name = "Test Product")
    {
        var product = new Product
        {
            Id         = id,
            CompanyId  = companyId,
            CategoryId = categoryId,
            IsDeleted  = false,
            Translations =
            [
                new ProductTranslation
                {
                    LanguageCode = "es",
                    Name         = name,
                    ShortDescription = null,
                }
            ],
        };
        Db.Products.Add(product);
        await Db.SaveChangesAsync();
        return product;
    }

    protected async Task<BranchProduct> SeedBranchProductAsync(
        int id         = 100,
        int branchId   = 100,
        int productId  = 100,
        int categoryId = 100,
        decimal price  = 1000m,
        bool isVisible = true)
    {
        var bp = new BranchProduct
        {
            Id                  = id,
            BranchId            = branchId,
            ProductId           = productId,
            CategoryId          = categoryId,
            Price               = price,
            IsVisible           = isVisible,
            IsDeleted           = false,
            DisplayOrder        = 1,
            ImageObjectFit      = "cover",
            ImageObjectPosition = "50% 50%",
        };
        Db.BranchProducts.Add(bp);
        await Db.SaveChangesAsync();
        return bp;
    }

    protected async Task<Tag> SeedTagAsync(
        int id = 100,
        int companyId = 100,
        string color = "#ffffff",
        string translationName = "Test Tag",
        string langCode = "es")
    {
        var tag = new Tag
        {
            Id        = id,
            CompanyId = companyId,
            Color     = color,
            IsDeleted = false,
            Translations =
            [
                new TagTranslation { TagId = id, LanguageCode = langCode, Name = translationName }
            ],
        };
        Db.Tags.Add(tag);
        await Db.SaveChangesAsync();
        return tag;
    }

    public void Dispose() => DbFactory.Dispose();
}
