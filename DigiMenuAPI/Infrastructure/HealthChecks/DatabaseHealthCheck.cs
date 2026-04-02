using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DigiMenuAPI.Infrastructure.HealthChecks
{
    public sealed class DatabaseHealthCheck : IHealthCheck
    {
        private readonly ApplicationDbContext _context;

        public DatabaseHealthCheck(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            try
            {
                var canConnect = await _context.Database.CanConnectAsync(cts.Token);
                return canConnect
                    ? HealthCheckResult.Healthy()
                    : HealthCheckResult.Unhealthy("No se pudo establecer conexión con la base de datos.");
            }
            catch (OperationCanceledException)
            {
                return HealthCheckResult.Unhealthy("Timeout al verificar la conexión con la base de datos (>5s).");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Error de base de datos: {ex.Message}");
            }
        }
    }
}
