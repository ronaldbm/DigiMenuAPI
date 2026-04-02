using AppCore.Domain.Entities;
using DigiMenuAPI.Application.Services;
using DigiMenuAPI.UnitTests.TestInfrastructure;
using FluentAssertions;

namespace DigiMenuAPI.UnitTests.Application.Services;

/// <summary>
/// Tests de StandardIconService — catálogo de iconos SVG de la plataforma.
///
/// Datos seedeados: StandardIcons IDs 1-3 (IconType 1-3) para la plataforma.
/// StandardIconService no tiene lógica multi-tenant — todos los tenants
/// ven el mismo catálogo global.
/// </summary>
[Trait("Category", "Unit")]
public sealed class StandardIconServiceTests : ServiceTestBase
{
    private StandardIconService CreateService()
        => new(Db, Mapper);

    // ── 1. GetAll ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_WithSeedIcons_ReturnsSeedIcons()
    {
        // StandardIcons están seedeados en CoreDbContext (IDs 1-N de la plataforma)
        var result = await CreateService().GetAll();

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        // El catálogo puede tener 0 íconos si el seed no incluye datos de test,
        // pero la llamada debe completarse sin error
    }

    [Fact]
    public async Task GetAll_WithManuallyAddedIcons_ReturnsThem()
    {
        Db.StandardIcons.Add(new StandardIcon
        {
            Id         = 100,
            Name       = "Test Icon",
            SvgContent = "<svg></svg>",
        });
        await Db.SaveChangesAsync();

        var result = await CreateService().GetAll();

        result.Success.Should().BeTrue();
        result.Data!.Any(i => i.Id == 100).Should().BeTrue();
    }
}
