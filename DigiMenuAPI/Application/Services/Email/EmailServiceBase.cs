using DigiMenuAPI.Application.Interfaces;

namespace DigiMenuAPI.Application.Services.Email
{
    /// <summary>
    /// Clase base para implementaciones de IEmailService.
    /// Solo expone SendRawAsync — el renderizado de templates
    /// ocurre en EmailQueueService al momento de encolar.
    /// </summary>
    public abstract class EmailServiceBase : IEmailService
    {
        protected readonly IConfiguration Config;
        protected readonly ILogger Logger;

        protected string FromEmail => Config["Email:FromEmail"] ?? "noreply@digimenu.cr";
        protected string FromName => Config["Email:FromName"] ?? "DigiMenu";

        protected EmailServiceBase(IConfiguration config, ILogger logger)
        {
            Config = config;
            Logger = logger;
        }

        /// <inheritdoc/>
        public async Task SendRawAsync(
            string toEmail, string toName, string subject, string htmlBody)
            => await SendAsync(toEmail, toName, subject, htmlBody);

        /// <summary>
        /// Implementación de transporte específica por proveedor.
        /// Debe manejar sus propias excepciones — un fallo nunca
        /// debe propagarse al EmailOutboxProcessor.
        /// </summary>
        protected abstract Task SendAsync(
            string toEmail,
            string toName,
            string subject,
            string htmlBody);
    }
}