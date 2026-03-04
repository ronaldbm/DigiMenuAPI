using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Empresa / Tenant. Cada cadena de restaurantes, bar o negocio
    /// es una Company independiente dentro de la plataforma.
    /// Una Company puede tener múltiples sucursales (Branch).
    ///
    /// Límites controlados por el plan contratado:
    ///   MaxBranches → cuántas sucursales puede crear
    ///   MaxUsers    → pool total de usuarios distribuibles entre sus branches
    /// El SuperAdmin puede modificar estos valores al cambiar el plan.
    /// </summary>
    public class Company
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;

        [Required, MaxLength(150)]
        public string Email { get; set; } = null!;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(3)]
        public string? CountryCode { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }

        // ── Plan y límites ────────────────────────────────────────────
        /// <summary>
        /// Plan de suscripción activo. Define los límites base,
        /// pero MaxBranches y MaxUsers en esta entidad son la fuente de verdad
        /// (permiten ajustes puntuales sin cambiar el plan).
        /// </summary>
        public int PlanId { get; set; }
        public Plan Plan { get; set; } = null!;

        /// <summary>Máximo de sucursales permitidas. -1 = ilimitado.</summary>
        public int MaxBranches { get; set; } = 1;

        /// <summary>
        /// Pool total de usuarios de la empresa (CompanyAdmin + BranchAdmins + Staff).
        /// -1 = ilimitado.
        /// </summary>
        public int MaxUsers { get; set; } = 3;

        // ── Navegación ───────────────────────────────────────────────
        public ICollection<Branch> Branches { get; set; } = new List<Branch>();
        public ICollection<AppUser> Users { get; set; } = new List<AppUser>();

        // Catálogo global: categorías, productos y tags son de la Company,
        // no de una Branch específica. Las Branches los "activan" via BranchProduct.
        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();

        public ICollection<CompanyModule> CompanyModules { get; set; } = new List<CompanyModule>();
    }
}
