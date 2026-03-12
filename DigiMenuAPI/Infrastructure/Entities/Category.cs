using AppCore.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Categoría del catálogo global de la Company.
    /// El CompanyAdmin las crea y son compartidas entre todas sus Branches.
    /// Cada BranchProduct referencia la categoría en la que aparece dentro de su Branch,
    /// permitiendo que el mismo producto esté en categorías diferentes por sucursal.
    /// </summary>
    public class Category : BaseEntity
    {
        // ── Multi-Tenant ─────────────────────────────────────────────
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;

        public int DisplayOrder { get; set; }
        public bool IsVisible { get; set; }
        public bool IsDeleted { get; set; }

        // ── Navegación ───────────────────────────────────────────────
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<BranchProduct> BranchProducts { get; set; } = new List<BranchProduct>();
        public ICollection<CategoryTranslation> Translations { get; set; } = new List<CategoryTranslation>();
    }
}
