using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Infrastructure.Email
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
    /// </summary>
    public class EmailOutboxProcessor : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<EmailOutboxProcessor> _logger;
        private readonly IConfiguration _config;

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
                    await ProcessBatchAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[EmailOutbox] Error inesperado en el ciclo de procesamiento.");
                }

                await Task.Delay(TimeSpan.FromSeconds(IntervalSeconds), stoppingToken);
            }
        }

        private async Task ProcessBatchAsync(CancellationToken ct)
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
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
            ApplicationDbContext context,
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