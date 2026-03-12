// Infrastructure/Entities/PasswordResetRequest.cs
using System.ComponentModel.DataAnnotations;
using AppCore.Domain.Entities;

namespace AppCore.Infrastructure.Entities
{
    /// <summary>
    /// Solicitud de recuperación de contraseña.
    ///
    /// Flujo:
    ///   1. ForgotPassword → crea registro con token único
    ///   2. Email enviado con link: {AppUrl}/reset-password?token={token}
    ///   3. ValidateResetToken → verifica que el token sea válido
    ///   4. ResetPassword → aplica nueva contraseña, IsUsed = true
    ///
    /// Seguridad:
    ///   - Token es Guid sin guiones (32 chars) — no predecible
    ///   - Expira en 1 hora desde CreatedAt
    ///   - IsUsed = true tras usarse — no reutilizable
    ///   - Tokens anteriores se invalidan al generar uno nuevo
    /// </summary>
    public class PasswordResetRequest : BaseEntity
    {
        // ── Multi-Tenant ──────────────────────────────────────────────
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        public int UserId { get; set; }
        public AppUser User { get; set; } = null!;

        /// <summary>Guid sin guiones generado con Guid.NewGuid().ToString("N").</summary>
        [Required, MaxLength(32)]
        public string Token { get; set; } = null!;

        /// <summary>CreatedAt + 1 hora.</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>True cuando el token ya fue usado. No puede reutilizarse.</summary>
        public bool IsUsed { get; set; }
    }
}
