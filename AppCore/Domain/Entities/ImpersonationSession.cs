using System.ComponentModel.DataAnnotations;

namespace AppCore.Domain.Entities
{
    /// <summary>
    /// Audit log de sesiones de impersonación.
    /// Cuando el SuperAdmin accede a una cuenta de tenant para soporte técnico,
    /// se registra aquí. El token nunca se almacena en claro — solo su hash SHA-256.
    ///
    /// Características de seguridad:
    ///   - Token de un solo uso: UsedAt se marca en la primera validación (transacción atómica)
    ///   - TTL fijo de 30 minutos: ExpiresAt = IssuedAt + 30 min
    ///   - TokenHash: SHA-256 del token aleatorio (32 bytes). Nunca en claro en BD
    ///   - IpAddress: IP del SuperAdmin al momento de generar el token
    ///
    /// No hereda de BaseEntity: el audit log no debe tener soft delete ni
    /// campos de ModifiedAt — es un registro inmutable de acciones de seguridad.
    /// </summary>
    public class ImpersonationSession
    {
        [Key]
        public int Id { get; set; }

        /// <summary>SuperAdmin que generó la sesión de impersonación.</summary>
        public int SuperAdminUserId { get; set; }
        public AppUser SuperAdmin { get; set; } = null!;

        /// <summary>Empresa a la que se accedió como soporte.</summary>
        public int TargetCompanyId { get; set; }
        public Company TargetCompany { get; set; } = null!;

        /// <summary>
        /// CompanyAdmin del tenant que se usó para emitir el JWT de impersonación.
        /// Se selecciona el primer CompanyAdmin activo de la empresa objetivo.
        /// </summary>
        public int TargetUserId { get; set; }
        public AppUser TargetUser { get; set; } = null!;

        /// <summary>
        /// SHA-256 del token en claro. El token nunca se persiste en texto plano.
        /// Longitud fija: 64 caracteres hex (SHA-256 de 32 bytes aleatorios).
        /// </summary>
        [Required, MaxLength(64)]
        public string TokenHash { get; set; } = null!;

        /// <summary>Momento en que el SuperAdmin generó el token.</summary>
        public DateTime IssuedAt { get; set; }

        /// <summary>IssuedAt + 30 minutos. El exchange falla si DateTime.UtcNow > ExpiresAt.</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Momento en que DigiMenuWeb consumió el token.
        /// Null = aún no usado. One-time use: una vez marcado, el token es inválido.
        /// </summary>
        public DateTime? UsedAt { get; set; }

        /// <summary>IP del SuperAdmin al momento de generar el token. Para auditoría.</summary>
        [Required, MaxLength(45)]
        public string IpAddress { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
    }
}
