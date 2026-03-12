using AppCore.Application.DTOs.Email;

namespace AppCore.Application.Interfaces
{
    /// <summary>
    /// Contrato para encolar correos en OutboxEmails.
    ///
    /// Los servicios de negocio (AuthService, ReservationService, etc.)
    /// llaman aquí en lugar de IEmailService directamente.
    /// Garantiza que el email quede persistido en DB antes de cualquier
    /// intento de envío — ningún correo se pierde aunque el servidor caiga.
    ///
    /// El envío real lo hace EmailOutboxProcessor en background.
    /// </summary>
    public interface IEmailQueueService
    {
        Task QueueWelcomeAsync(WelcomeEmailDto dto, int companyId, int? branchId = null);
        Task QueueTemporaryPasswordAsync(TemporaryPasswordEmailDto dto, int companyId, int? branchId = null);
        Task QueueForgotPasswordAsync(ForgotPasswordEmailDto dto, int companyId, int? branchId = null);
        Task QueueReservationConfirmationAsync(ReservationConfirmationEmailDto dto, int companyId, int branchId);
    }
}
