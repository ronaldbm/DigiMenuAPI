using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Activación de un producto del catálogo global en una sucursal específica.
    /// Permite que cada Branch tenga su propio precio, visibilidad, orden e imagen.
    ///
    /// Si existe un BranchProduct activo (IsVisible = true, IsDeleted = false)
    /// el producto aparece en el menú público de esa Branch.
    /// Si no existe o está inactivo, el producto no se muestra en esa sucursal.
    ///
    /// Ejemplo:
    ///   Producto:        "Hamburguesa Clásica" (catálogo global)
    ///   Branch Centro:   BranchProduct → precio ₡4,500, categoría "Platos Fuertes"
    ///   Branch Norte:    BranchProduct → precio ₡4,800, categoría "Burgers"
    ///   Branch Mall:     Sin BranchProduct → no aparece en el menú del Mall
    /// </summary>
    public class BranchProduct : BaseEntity
    {
        // ── Multi-Tenant ─────────────────────────────────────────────
        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        // ── Producto del catálogo global ──────────────────────────────
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        /// <summary>
        /// Categoría en la que aparece este producto dentro de esta Branch.
        /// Puede diferir de la categoría base del Product global.
        /// </summary>
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        // ── Configuración por sucursal ────────────────────────────────
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? OfferPrice { get; set; }

        /// <summary>
        /// Imagen opcional que sobreescribe la imagen base del Product global.
        /// Si es null, se usa Product.MainImageUrl.
        /// </summary>
        public string? ImageOverrideUrl { get; set; }

        public int DisplayOrder { get; set; }
        public bool IsVisible { get; set; }
        public bool IsDeleted { get; set; }
    }
}
