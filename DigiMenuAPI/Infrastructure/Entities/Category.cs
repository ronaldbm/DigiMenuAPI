using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    public class Category : BaseEntity
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;

        public int DisplayOrder { get; set; }
        public bool IsVisible { get; set; }
        public bool IsDeleted { get; set; }

        // ── Multi-Tenant ─────────────────────────────────────────────
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        // ── Relaciones ───────────────────────────────────────────────
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}