using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Producto del catálogo global de la Company.
    /// El CompanyAdmin lo crea con nombre, descripción e imagen base.
    ///
    /// El producto NO tiene precio aquí porque cada sucursal puede
    /// tener un precio diferente. El precio, visibilidad y orden
    /// se definen en BranchProduct al activar el producto en una Branch.
    ///
    /// Para que un producto aparezca en el menú público de una sucursal,
    /// debe existir un BranchProduct activo que lo vincule.
    /// </summary>
    public class Product : BaseEntity
    {
        // ── Multi-Tenant ─────────────────────────────────────────────
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        // ── Catálogo global ───────────────────────────────────────────
        [Required, MaxLength(150)]
        public string Name { get; set; } = null!;

        public string? ShortDescription { get; set; }
        public string? LongDescription { get; set; }

        /// <summary>Imagen base del producto. Cada Branch puede sobreescribirla en BranchProduct.</summary>
        public string? MainImageUrl { get; set; }

        /// <summary>
        /// Categoría base del producto en el catálogo global.
        /// Cada Branch puede asignarlo a una categoría diferente en BranchProduct.
        /// </summary>
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public bool IsDeleted { get; set; }

        // ── Muchos a Muchos con Tags ──────────────────────────────────
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();

        // ── Navegación ───────────────────────────────────────────────
        public ICollection<BranchProduct> BranchProducts { get; set; } = new List<BranchProduct>();
        public ICollection<ProductTranslation> Translations { get; set; } = new List<ProductTranslation>();
    }
}
