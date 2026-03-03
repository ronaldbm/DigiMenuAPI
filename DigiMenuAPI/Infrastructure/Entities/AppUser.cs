using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Usuario del sistema. Siempre pertenece a una Company.
    /// Roles: 1=SuperAdmin (plataforma), 2=Admin (empresa), 3=Staff
    /// </summary>
    public class AppUser : BaseEntity
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; } = null!;

        [Required, MaxLength(150)]
        public string Email { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!;

        /// <summary>1=SuperAdmin, 2=Admin, 3=Staff</summary>
        public byte Role { get; set; } = 2;

        public bool IsActive { get; set; } = true;

        // ── TENANT ──────────────────────────────────────────────────
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;
    }
}