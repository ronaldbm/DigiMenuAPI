using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Empresa / Tenant raíz del sistema SaaS.
    /// Todo registro de negocio pertenece a una Company.
    /// </summary>
    public class Company : BaseEntity
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;

        /// <summary>Slug único global. Usado en la URL pública del menú: /menu/{slug}</summary>
        [Required, MaxLength(60)]
        public string Slug { get; set; } = null!;

        [Required, MaxLength(150)]
        public string Email { get; set; } = null!;

        [MaxLength(20)]
        public string? Phone { get; set; }

        /// <summary>País de la empresa (ej: "CO", "MX", "CR")</summary>
        [MaxLength(3)]
        public string? CountryCode { get; set; }

        public bool IsActive { get; set; } = true;

        // ── NAVEGACIÓN ──────────────────────────────────────────────
        public ICollection<AppUser>       Users        { get; set; } = new List<AppUser>();
        public Setting?                   Setting      { get; set; }
        public ICollection<Category>      Categories   { get; set; } = new List<Category>();
        public ICollection<Product>       Products     { get; set; } = new List<Product>();
        public ICollection<Tag>           Tags         { get; set; } = new List<Tag>();
        public ICollection<FooterLink>    FooterLinks  { get; set; } = new List<FooterLink>();
        public ICollection<Reservation>   Reservations { get; set; } = new List<Reservation>();
        public ICollection<CompanyModule> Modules      { get; set; } = new List<CompanyModule>();
    }
}
