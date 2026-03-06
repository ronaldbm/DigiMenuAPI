using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Usuario del sistema.
    ///
    /// Roles y alcance:
    ///   255 = SuperAdmin  → acceso total a la plataforma. BranchId = null.
    ///   254 = SuperAdminCompany → acceso total a una Company específica. BranchId = null.
    ///     1 = CompanyAdmin → gestiona su Company y todas sus Branches. BranchId = null.
    ///     2 = BranchAdmin  → gestiona solo su Branch asignada. BranchId requerido.
    ///     3 = Staff        → acceso operativo limitado a su Branch. BranchId requerido.
    ///
    /// Jerarquía de creación:
    ///   SuperAdminCompany → crea CompanyAdmins para su propia Company
    ///   SuperAdmin    → crea CompanyAdmins
    ///   CompanyAdmin  → crea BranchAdmins (asignados a una Branch)
    ///   BranchAdmin   → crea Staff (solo en su Branch)
    ///
    /// El límite de usuarios por empresa se controla con Company.MaxUsers,
    /// contando todos los usuarios activos y no eliminados de esa Company.
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

        /// <summary>255 = SuperAdmin | 254 = SuperAdminCompany | 1 = CompanyAdmin | 2 = BranchAdmin | 3 = Staff</summary>
        public byte Role { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }

        public DateTime? LastLoginAt { get; set; }

        // ── Multi-Tenant ─────────────────────────────────────────────
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        /// <summary>
        /// Sucursal asignada. Null para SuperAdmin y CompanyAdmin,
        /// requerido para BranchAdmin y Staff.
        /// </summary>
        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }
    }
}
