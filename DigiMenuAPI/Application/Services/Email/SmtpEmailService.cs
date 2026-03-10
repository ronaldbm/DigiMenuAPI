using System.Net;
using System.Net.Mail;

namespace DigiMenuAPI.Application.Services.Email
{
    /// <summary>
    /// Implementación de IEmailService usando SMTP estándar.
    ///
    /// Compatible con:
    ///   Gmail:        smtp.gmail.com:587       (requiere App Password)
    ///   Outlook/O365: smtp.office365.com:587
    ///   Amazon SES:   email-smtp.{region}.amazonaws.com:587
    ///   Propio:       cualquier host/puerto SMTP
    ///
    /// Configuración requerida en appsettings.json:
    ///   "Email": {
    ///     "Provider":  "Smtp",
    ///     "FromEmail": "noreply@miempresa.com",
    ///     "FromName":  "Mi Empresa",
    ///     "Smtp": {
    ///       "Host":      "smtp.gmail.com",
    ///       "Port":      587,
    ///       "User":      "cuenta@gmail.com",
    ///       "Password":  "app-password",
    ///       "EnableSsl": true
    ///     }
    ///   }
    /// </summary>
    public class SmtpEmailService : EmailServiceBase
    {
        private string SmtpHost => Config["Email:Smtp:Host"]
            ?? throw new InvalidOperationException("Email:Smtp:Host no configurado.");
        private int SmtpPort => int.Parse(Config["Email:Smtp:Port"] ?? "587");
        private string SmtpUser => Config["Email:Smtp:User"]
            ?? throw new InvalidOperationException("Email:Smtp:User no configurado.");
        private string SmtpPassword => Config["Email:Smtp:Password"]
            ?? throw new InvalidOperationException("Email:Smtp:Password no configurado.");
        private bool EnableSsl => bool.Parse(Config["Email:Smtp:EnableSsl"] ?? "true");

        public SmtpEmailService(
            IConfiguration config,
            ILogger<SmtpEmailService> logger)
            : base(config, logger) { }

        protected override async Task SendAsync(
            string toEmail, string toName, string subject, string htmlBody)
        {
            try
            {
                using var client = new SmtpClient(SmtpHost, SmtpPort)
                {
                    Credentials = new NetworkCredential(SmtpUser, SmtpPassword),
                    EnableSsl = EnableSsl
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(FromEmail, FromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                message.To.Add(new MailAddress(toEmail, toName));

                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    "[Smtp] Excepción al enviar a {Email} via {Host}:{Port}",
                    toEmail, SmtpHost, SmtpPort);
                throw; // Propagar para que el processor gestione el reintento
            }
        }
    }
}