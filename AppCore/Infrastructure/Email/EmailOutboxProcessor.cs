using AppCore.Application.Interfaces;
using AppCore.Infrastructure.Entities;
using AppCore.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;

namespace AppCore.Infrastructure.Email
{
    /// <summary>
    /// Background Job que procesa la cola de correos pendientes.
    ///
    /// Configuración en appsettings.json (sección Email:Outbox):
    ///   IntervalSeconds → cada cuántos segundos corre el job (default: 30)
    ///   MaxRetries      → intentos antes de Abandoned (default: 5)
    ///   BatchSize       → correos por ciclo (default: 20)
    ///
    /// Backoff exponencial: 2^(RetryCount-1) minutos entre reintentos.
    ///
    /// Circuit breaker integrado: tras N fallos consecutivos de BD,
    /// el processor se detiene por un periodo de cooldown antes de reintentar,
    /// evitando saturar el pool de conexiones cuando SQL Server no responde.
    /// </summary>
    public class EmailOutboxProcessor : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<EmailOutboxProcessor> _logger;
        private readonly IConfiguration _config;

        // ── Circuit Breaker State ─────────────────────────────────────────
        private int _consecutiveDbFailures;
        private const int CircuitBreakerThreshold = 3;
        private static readonly TimeSpan CircuitBreakerCooldown = TimeSpan.FromMinutes(5);

        private int IntervalSeconds => int.Parse(_config["Email:Outbox:IntervalSeconds"] ?? "30");
        private int MaxRetries => int.Parse(_config["Email:Outbox:MaxRetries"] ?? "5");
        private int BatchSize => int.Parse(_config["Email:Outbox:BatchSize"] ?? "20");

        public EmailOutboxProcessor(
            IServiceProvider services,
            ILogger<EmailOutboxProcessor> logger,
            IConfiguration config)
        {
            _services = services;
            _logger = logger;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "[EmailOutbox] Processor iniciado. Intervalo: {Interval}s | MaxRetries: {Max} | Batch: {Batch}",
                IntervalSeconds, MaxRetries, BatchSize);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // ── Circuit Breaker: si hay demasiados fallos consecutivos,
                    //    esperar un período largo antes de reintentar para no
                    //    saturar el pool de conexiones.
                    if (_consecutiveDbFailures >= CircuitBreakerThreshold)
                    {
                        _logger.LogWarning(
                            "[EmailOutbox] ⚡ Circuit breaker abierto tras {Failures} fallos consecutivos de BD. " +
                            "Esperando {Cooldown} antes de reintentar.",
                            _consecutiveDbFailures, CircuitBreakerCooldown);

                        await Task.Delay(CircuitBreakerCooldown, stoppingToken);
                        _consecutiveDbFailures = 0; // Reset para reintentar

                        _logger.LogInformation("[EmailOutbox] ⚡ Circuit breaker cerrado. Reintentando conexión.");
                    }

                    // ── Health Check rápido antes de procesar ──────────────────
                    if (!await IsDatabaseReachableAsync(stoppingToken))
                    {
                        _consecutiveDbFailures++;
                        _logger.LogWarning(
                            "[EmailOutbox] ⚠️ BD no alcanzable (fallo {N}/{Max}). Saltando ciclo.",
                            _consecutiveDbFailures, CircuitBreakerThreshold);

                        await Task.Delay(TimeSpan.FromSeconds(IntervalSeconds), stoppingToken);
                        continue;
                    }

                    // Si llegamos aquí, la BD respondió → reset del circuit breaker
                    _consecutiveDbFailures = 0;

                    await ProcessBatchAsync(stoppingToken);
                }
                catch (Exception ex) when (IsDbConnectionError(ex))
                {
                    _consecutiveDbFailures++;
                    _logger.LogError(ex,
                        "[EmailOutbox] Error de conexión a BD (fallo {N}/{Max}). Saltando ciclo.",
                        _consecutiveDbFailures, CircuitBreakerThreshold);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[EmailOutbox] Error inesperado en el ciclo de procesamiento.");
                }

                await Task.Delay(TimeSpan.FromSeconds(IntervalSeconds), stoppingToken);
            }
        }

        /// <summary>
        /// Verifica que la BD sea alcanzable con un SELECT 1 rápido (timeout 5s).
        /// No consume conexiones del pool de forma prolongada.
        /// </summary>
        private async Task<bool> IsDatabaseReachableAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

                // CanConnectAsync usa un timeout corto y libera la conexión inmediatamente
                return await context.Database.CanConnectAsync(ct);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Determina si la excepción es un error de conexión a BD
        /// (para distinguirlo de errores de lógica de negocio).
        /// </summary>
        private static bool IsDbConnectionError(Exception ex)
        {
            return ex is Microsoft.Data.SqlClient.SqlException
                || ex is Microsoft.EntityFrameworkCore.Storage.RetryLimitExceededException
                || ex is TimeoutException
                || ex.InnerException is Microsoft.Data.SqlClient.SqlException;
        }

        private async Task ProcessBatchAsync(CancellationToken ct)
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var now = DateTime.UtcNow;

            // Query liviano — solo IDs, sin cargar HtmlBody
            var emailIds = await context.OutboxEmails
                .Where(e =>
                    (e.Status == OutboxEmailStatus.Pending ||
                     e.Status == OutboxEmailStatus.Failed) &&
                    e.RetryCount < MaxRetries &&
                    (e.NextRetryAt == null || e.NextRetryAt <= now))
                .OrderBy(e => e.CreatedAt)
                .Take(BatchSize)
                .Select(e => e.Id)
                .ToListAsync(ct);

            if (!emailIds.Any()) return;

            _logger.LogInformation("[EmailOutbox] Procesando {Count} correos.", emailIds.Count);

            foreach (var id in emailIds)
            {
                // JOIN explícito solo al momento de enviar
                var email = await context.OutboxEmails
                    .Include(e => e.Body)
                    .FirstOrDefaultAsync(e => e.Id == id, ct);

                if (email is null || email.Body is null) continue;

                await ProcessSingleAsync(email, emailService, context, ct);
            }
        }

        private async Task ProcessSingleAsync(
            OutboxEmail email,
            IEmailService emailService,
            CoreDbContext context,
            CancellationToken ct)
        {
            email.LastAttemptAt = DateTime.UtcNow;
            email.RetryCount++;

            try
            {
                await emailService.SendRawAsync(
                    toEmail: email.ToEmail,
                    toName: email.ToName,
                    subject: email.Subject,
                    htmlBody: email.Body!.HtmlBody);

                email.Status = OutboxEmailStatus.Sent;
                email.SentAt = DateTime.UtcNow;
                email.LastError = null;
                email.NextRetryAt = null;

                _logger.LogInformation(
                    "[EmailOutbox] ✅ Enviado. ID: {Id} | Tipo: {Type} | Para: {Email}",
                    email.Id, email.EmailType, email.ToEmail);
            }
            catch (Exception ex)
            {
                var error = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;

                if (email.RetryCount >= MaxRetries)
                {
                    email.Status = OutboxEmailStatus.Abandoned;
                    email.LastError = $"[Abandoned tras {email.RetryCount} intentos] {error}";

                    _logger.LogError(
                        "[EmailOutbox] ❌ Abandonado. ID: {Id} | Para: {Email} | Error: {Error}",
                        email.Id, email.ToEmail, error);
                }
                else
                {
                    var backoffMinutes = Math.Pow(2, email.RetryCount - 1);
                    email.Status = OutboxEmailStatus.Failed;
                    email.LastError = error;
                    email.NextRetryAt = DateTime.UtcNow.AddMinutes(backoffMinutes);

                    _logger.LogWarning(
                        "[EmailOutbox] ⚠️ Fallo {Retry}/{Max}. ID: {Id} | Próximo: {Next} | Error: {Error}",
                        email.RetryCount, MaxRetries, email.Id, email.NextRetryAt, error);
                }
            }

            await context.SaveChangesAsync(ct);
        }
    }
}
