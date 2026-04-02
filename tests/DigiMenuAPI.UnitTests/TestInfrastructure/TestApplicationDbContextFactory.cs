using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using NSubstitute;

namespace DigiMenuAPI.UnitTests.TestInfrastructure;

/// <summary>
/// Factoría que produce instancias de ApplicationDbContext respaldadas por
/// EF Core InMemory para tests unitarios.
///
/// Decisiones de diseño:
///   - InMemory: soporta HasQueryFilter (soft delete) y no requiere SQL Server.
///   - Cada test recibe su propia base de datos (nombre único con Guid) para
///     garantizar aislamiento total entre tests paralelos.
///   - IHttpContextAccessor es un mock que retorna null: el audit trail
///     de CoreDbContext maneja null graciosamente (no establece userId).
///   - La instancia es IDisposable para liberar la base InMemory correctamente.
/// </summary>
public sealed class TestApplicationDbContextFactory : IDisposable
{
    private readonly ApplicationDbContext _context;
    private bool _disposed;

    public TestApplicationDbContextFactory(string? databaseName = null)
    {
        var dbName = databaseName ?? $"TestDb_{Guid.NewGuid():N}";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .EnableSensitiveDataLogging()
            // InMemory no soporta transacciones reales — suprimimos la advertencia
            // para que servicios que usan BeginTransactionAsync funcionen correctamente.
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        // IHttpContextAccessor con HttpContext null — audit trail devuelve null userId (OK).
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        _context = new ApplicationDbContext(options, httpContextAccessor);
        _context.Database.EnsureCreated();
    }

    public ApplicationDbContext Context => _context;

    public void Dispose()
    {
        if (!_disposed)
        {
            _context.Dispose();
            _disposed = true;
        }
    }
}
