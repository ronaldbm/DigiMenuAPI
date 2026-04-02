using AppCore.Infrastructure.SQL;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace AppCore.UnitTests.TestInfrastructure;

/// <summary>
/// Factoría de CoreDbContext (concreto anónimo) para tests unitarios del AppCore.
/// Usa EF Core InMemory para evitar dependencia de SQL Server.
/// </summary>
public sealed class CoreDbContextFactory : IDisposable
{
    private readonly ConcreteTestDbContext _context;
    private bool _disposed;

    public CoreDbContextFactory(string? databaseName = null)
    {
        var dbName = databaseName ?? $"CoreTestDb_{Guid.NewGuid():N}";

        var options = new DbContextOptionsBuilder<ConcreteTestDbContext>()
            .UseInMemoryDatabase(dbName)
            .EnableSensitiveDataLogging()
            .Options;

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        _context = new ConcreteTestDbContext(options, httpContextAccessor);
        _context.Database.EnsureCreated();
    }

    public CoreDbContext Context => _context;

    public void Dispose()
    {
        if (!_disposed)
        {
            _context.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Implementación concreta mínima de CoreDbContext (abstracto) para tests.
/// No agrega entidades extra — solo expone el modelo base.
/// </summary>
internal sealed class ConcreteTestDbContext : CoreDbContext
{
    public ConcreteTestDbContext(
        DbContextOptions<ConcreteTestDbContext> options,
        IHttpContextAccessor httpContextAccessor)
        : base(options, httpContextAccessor)
    {
    }
}
