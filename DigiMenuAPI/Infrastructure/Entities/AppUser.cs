using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Usuario del sistema.
    /// Roles disponibles:
    ///   255 = SuperAdmin (plataforma completa, no ligado a un local específico)
    ///     1 = Admin (propietario/administrador de su empresa)
    ///     2 = Staff (empleado con acceso limitado a su empresa)
    /// </summary>
    public class AppUser : BaseEntity
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; } = null!;

        [Required, MaxLength(150)]
        public string Email { get; set; } = null!;

        /// <summary>
        /// Hash BCrypt. Nunca almacenar contraseña en texto plano.
        /// Generar con: BCrypt.Net.BCrypt.HashPassword(password, 12)
        /// </summary>
        [Required]
        public string PasswordHash { get; set; } = null!;

        /// <summary>255 = SuperAdmin | 1 = Admin | 2 = Staff</summary>
        public byte Role { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? LastLoginAt { get; set; }

        // ── Multi-Tenant ─────────────────────────────────────────────
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;
    }
}