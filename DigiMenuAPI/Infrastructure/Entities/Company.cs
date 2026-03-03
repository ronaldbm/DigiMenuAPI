using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Empresa / Tenant. Cada restaurante, bar o local
    /// es una Company independiente dentro de la plataforma.
    /// El Slug es la clave pública para la URL del menú: digimenu.app/{slug}
    /// </summary>
    public class Company
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Identificador único en URL. Ejemplo: "el-rancho", "bar-luna"
        /// </summary>
        [Required, MaxLength(60)]
        public string Slug { get; set; } = null!;

        [Required, MaxLength(150)]
        public string Email { get; set; } = null!;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(3)]
        public string? CountryCode { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }

        // ── Navegación ───────────────────────────────────────────────
        public Setting? Setting { get; set; }
        public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
        public ICollection<FooterLink> FooterLinks { get; set; } = new List<FooterLink>();
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
        public ICollection<CompanyModule> CompanyModules { get; set; } = new List<CompanyModule>();
    }
}