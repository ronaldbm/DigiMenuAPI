using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;

    public TenantService(
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
    }

    public async Task<int?> ResolveCompanyBySlugAsync(string slug)
    {
        // Se resuelve solo cuando se necesita, rompiendo la circularidad
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Companies
            .AsNoTracking()
            .Where(c => c.Slug == slug.ToLower().Trim() && c.IsActive)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync();
    }

    public int GetCompanyId()
    {
        var value = TryGetCompanyId();
        if (value is null)
            throw new UnauthorizedAccessException(
                "No se pudo determinar la empresa del usuario autenticado.");
        return value.Value;
    }

    public int? TryGetCompanyId()
    {
        var claim = GetClaim("companyId");
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id))
            return null;
        return id;
    }

    public byte GetUserRole()
    {
        var claim = GetClaim("role");
        if (string.IsNullOrEmpty(claim) || !byte.TryParse(claim, out var role))
            return 0;
        return role;
    }

    private string? GetClaim(string claimType)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user is null || user.Identity?.IsAuthenticated != true)
            return null;
        return user.FindFirstValue(claimType);
    }
}