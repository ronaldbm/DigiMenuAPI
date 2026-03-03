using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiMenuAPI.Infrastructure.Entities
{
    public class Product : BaseEntity
    {

        [Required, MaxLength(150)]
        public string Name { get; set; } = null!;

        public string? ShortDescription { get; set; }
        public string? LongDescription { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BasePrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? OfferPrice { get; set; }

        public string? MainImageUrl { get; set; }

        public int DisplayOrder { get; set; }
        public bool IsVisible { get; set; }
        public bool IsDeleted { get; set; }

        // ── Multi-Tenant ─────────────────────────────────────────────
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        // ── Relaciones ───────────────────────────────────────────────
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        // Muchos a Muchos con Tags
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    }
}