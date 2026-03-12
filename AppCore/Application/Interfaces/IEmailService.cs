namespace AppCore.Application.Interfaces
{
    /// <summary>
    /// Contrato de transporte de correos electrónicos.
    /// Las implementaciones se seleccionan por Email:Provider en appsettings.
    ///
    /// Los servicios de negocio NO llaman a IEmailService directamente —
    /// usan IEmailQueueService para garantizar persistencia en DB.
    /// IEmailService es usado exclusivamente por EmailOutboxProcessor.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Envía un correo con HTML ya renderizado.
        /// Usado exclusivamente por EmailOutboxProcessor.
        /// </summary>
        Task SendRawAsync(string toEmail, string toName, string subject, string htmlBody);
    }
}
