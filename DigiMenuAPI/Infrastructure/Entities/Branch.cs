using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Sucursal / localización física de una Company.
    /// Cada Branch tiene su propio menú (BranchProducts), configuración visual,
    /// reservas, footer links y usuarios de staff.
    ///
    /// El Slug es globalmente único y define la URL pública del menú:
    ///   digimenu.app/{slug}
    ///
    /// Jerarquía:
    ///   Company (El Rancho S.A.)
    ///     └── Branch (Sucursal Centro)   → digimenu.app/el-rancho-centro
    ///     └── Branch (Sucursal Norte)    → digimenu.app/el-rancho-norte
    /// </summary>
    public class Branch : BaseEntity
    {
        // ── Multi-Tenant ─────────────────────────────────────────────
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        // ── Datos de la sucursal ──────────────────────────────────────
        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Identificador único global para la URL pública del menú.
        /// Ejemplo: "el-rancho-centro", "bar-luna-mall"
        /// </summary>
        [Required, MaxLength(60)]
        public string Slug { get; set; } = null!;

        [MaxLength(200)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(150)]
        public string? Email { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }

        // ── Navegación ───────────────────────────────────────────────
        /// <summary>Configuración visual y de comportamiento propia de esta sucursal.</summary>
        public Setting? Setting { get; set; }
        public ICollection<BranchProduct> BranchProducts { get; set; } = new List<BranchProduct>();
        public ICollection<FooterLink> FooterLinks { get; set; } = new List<FooterLink>();
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

        /// <summary>
        /// Usuarios asignados a esta Branch (BranchAdmin y Staff).
        /// El CompanyAdmin no pertenece a ninguna Branch (BranchId = null en AppUser).
        /// </summary>
        public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
    }
}
