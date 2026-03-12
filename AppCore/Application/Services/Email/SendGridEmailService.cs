using SendGrid;
using SendGrid.Helpers.Mail;

namespace AppCore.Application.Services.Email
{
    /// <summary>
    /// Implementación de IEmailService usando SendGrid API.
    ///
    /// Configuración requerida en appsettings.json:
    ///   "Email": {
    ///     "Provider":       "SendGrid",
    ///     "SendGridApiKey": "SG.xxx",
    ///     "FromEmail":      "noreply@digimenu.cr",
    ///     "FromName":       "DigiMenu"
    ///   }
    /// </summary>
    public class SendGridEmailService : EmailServiceBase
    {
        private string ApiKey => Config["Email:SendGridApiKey"]
            ?? throw new InvalidOperationException("Email:SendGridApiKey no configurado.");

        public SendGridEmailService(
            IConfiguration config,
            ILogger<SendGridEmailService> logger)
            : base(config, logger) { }

        protected override async Task SendAsync(
            string toEmail, string toName, string subject, string htmlBody)
        {
            try
            {
                var client = new SendGridClient(ApiKey);
                var from = new EmailAddress(FromEmail, FromName);
                var to = new EmailAddress(toEmail, toName);
                var message = MailHelper.CreateSingleEmail(
                    from, to, subject,
                    plainTextContent: null,
                    htmlContent: htmlBody);

                var response = await client.SendEmailAsync(message);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Body.ReadAsStringAsync();
                    Logger.LogError(
                        "[SendGrid] Error al enviar a {Email}. Status: {Status}. Body: {Body}",
                        toEmail, response.StatusCode, body);

                    // Lanzar para que el processor registre el fallo y reintente
                    throw new Exception($"SendGrid respondió {response.StatusCode}: {body}");
                }
            }
            catch (Exception ex) when (ex.Message.StartsWith("SendGrid"))
            {
                throw; // Propagar para que el processor gestione el reintento
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[SendGrid] Excepción al enviar a {Email}", toEmail);
                throw; // Propagar para que el processor gestione el reintento
            }
        }
    }
}
