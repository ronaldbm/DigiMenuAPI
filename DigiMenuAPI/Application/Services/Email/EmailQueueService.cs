using DigiMenuAPI.Application.DTOs.Email;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.Email;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.Infrastructure.SQL;
using DigiMenuIC.Application.Interfaces;

namespace DigiMenuAPI.Application.Services.Email
{
    /// <summary>
    /// Encola correos en OutboxEmails renderizando el HTML en el momento
    /// del encolado. Así el contenido queda guardado exactamente como
    /// se enviará, independientemente de cambios futuros en los templates.
    /// </summary>
    public class EmailQueueService : IEmailQueueService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<EmailQueueService> _logger;

        private string AppUrl => _config["Email:AppUrl"] ?? "https://app.digimenu.cr";

        public EmailQueueService(
            ApplicationDbContext context,
            IConfiguration config,
            ILogger<EmailQueueService> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }

        public async Task QueueWelcomeAsync(
            WelcomeEmailDto dto, int companyId, int? branchId = null)
        {
            var html = await EmailTemplateRenderer.RenderAsync("welcome", new Dictionary<string, string>
            {
                { "{{ADMIN_FULL_NAME}}", dto.AdminFullName },
                { "{{COMPANY_NAME}}",    dto.CompanyName },
                { "{{LOGIN_URL}}",       dto.LoginUrl },
                { "{{YEAR}}",            DateTime.UtcNow.Year.ToString() }
            });

            await EnqueueAsync(
                toEmail: dto.ToEmail,
                toName: dto.AdminFullName,
                subject: $"¡Bienvenido a DigiMenu, {dto.CompanyName}!",
                htmlBody: html,
                emailType: OutboxEmailType.Welcome,
                companyId: companyId,
                branchId: branchId);
        }

        public async Task QueueTemporaryPasswordAsync(
            TemporaryPasswordEmailDto dto, int companyId, int? branchId = null)
        {
            var html = await EmailTemplateRenderer.RenderAsync("temporary-password", new Dictionary<string, string>
            {
                { "{{FULL_NAME}}",      dto.FullName },
                { "{{COMPANY_NAME}}",   dto.CompanyName },
                { "{{TEMP_PASSWORD}}", dto.TemporaryPassword },
                { "{{LOGIN_URL}}",      dto.LoginUrl },
                { "{{YEAR}}",           DateTime.UtcNow.Year.ToString() }
            });

            await EnqueueAsync(
                toEmail: dto.ToEmail,
                toName: dto.FullName,
                subject: $"Tu acceso a DigiMenu — {dto.CompanyName}",
                htmlBody: html,
                emailType: OutboxEmailType.TemporaryPassword,
                companyId: companyId,
                branchId: branchId);
        }

        public async Task QueueForgotPasswordAsync(
            ForgotPasswordEmailDto dto, int companyId, int? branchId = null)
        {
            var html = await EmailTemplateRenderer.RenderAsync("forgot-password", new Dictionary<string, string>
            {
                { "{{FULL_NAME}}",   dto.FullName },
                { "{{RESET_URL}}",   dto.ResetUrl },
                { "{{EXPIRES_AT}}", dto.ExpiresAt.ToString("dd/MM/yyyy HH:mm") + " UTC" },
                { "{{YEAR}}",        DateTime.UtcNow.Year.ToString() }
            });

            await EnqueueAsync(
                toEmail: dto.ToEmail,
                toName: dto.FullName,
                subject: "Recuperación de contraseña — DigiMenu",
                htmlBody: html,
                emailType: OutboxEmailType.ForgotPassword,
                companyId: companyId,
                branchId: branchId);
        }

        public async Task QueueReservationConfirmationAsync(
            ReservationConfirmationEmailDto dto, int companyId, int branchId)
        {
            var notesRow = dto.Notes is not null
                ? $"""
                  <tr><td style="padding:12px;background:#f8f9fa;border-bottom:1px solid #e0e0e0;">
                  <span style="color:#888;font-size:12px;">NOTAS</span><br/>
                  <strong style="color:#1D3557;">{dto.Notes}</strong></td></tr>
                  """
                : string.Empty;

            var phoneRow = dto.BusinessPhone is not null
                ? $"""
                  <tr><td style="padding:12px;background:#f8f9fa;border-bottom:1px solid #e0e0e0;">
                  <span style="color:#888;font-size:12px;">TELÉFONO</span><br/>
                  <strong style="color:#1D3557;">{dto.BusinessPhone}</strong></td></tr>
                  """
                : string.Empty;

            var addressRow = dto.BusinessAddress is not null
                ? $"""
                  <tr><td style="padding:12px;background:#f8f9fa;border-radius:0 0 4px 4px;">
                  <span style="color:#888;font-size:12px;">DIRECCIÓN</span><br/>
                  <strong style="color:#1D3557;">{dto.BusinessAddress}</strong></td></tr>
                  """
                : string.Empty;

            var html = await EmailTemplateRenderer.RenderAsync("reservation-confirmation", new Dictionary<string, string>
            {
                { "{{CLIENT_NAME}}",       dto.ClientName },
                { "{{BUSINESS_NAME}}",     dto.BusinessName },
                { "{{RESERVATION_DATE}}", dto.ReservationDate.ToString("dddd, dd 'de' MMMM 'de' yyyy") },
                { "{{RESERVATION_TIME}}", dto.ReservationTime },
                { "{{GUEST_COUNT}}",       dto.GuestCount + " persona(s)" },
                { "{{NOTES_ROW}}",         notesRow },
                { "{{PHONE_ROW}}",         phoneRow },
                { "{{ADDRESS_ROW}}",       addressRow },
                { "{{YEAR}}",              DateTime.UtcNow.Year.ToString() }
            });

            await EnqueueAsync(
                toEmail: dto.ToEmail,
                toName: dto.ClientName,
                subject: $"Reserva confirmada en {dto.BusinessName}",
                htmlBody: html,
                emailType: OutboxEmailType.ReservationConfirmation,
                companyId: companyId,
                branchId: branchId);
        }

        // ── Helper base ───────────────────────────────────────────────

        private async Task EnqueueAsync(
            string toEmail,
            string toName,
            string subject,
            string htmlBody,
            OutboxEmailType emailType,
            int companyId,
            int? branchId)
        {
            var outbox = new OutboxEmail
            {
                CompanyId = companyId,
                BranchId = branchId,
                ToEmail = toEmail,
                ToName = toName,
                Subject = subject,
                EmailType = emailType,
                Status = OutboxEmailStatus.Pending,
                RetryCount = 0,
                NextRetryAt = null,
                Body = new OutboxEmailBody { HtmlBody = htmlBody }
            };

            _context.OutboxEmails.Add(outbox);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "[EmailQueue] Correo encolado. Tipo: {Type} | Para: {Email} | Company: {CompanyId}",
                emailType, toEmail, companyId);
        }
    }
}