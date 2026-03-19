using AppCore.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Etiqueta del catálogo global de la Company.
    /// El CompanyAdmin las crea y se asignan a los Products del catálogo global.
    /// Aparecen en el menú público de las Branches que tienen activo el BranchProduct correspondiente.
    /// El nombre vive exclusivamente en TagTranslation.
    /// </summary>
    public class Tag : BaseEntity
    {
        // ── Multi-Tenant ─────────────────────────────────────────────
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        /// <summary>Color en formato hex. Ejemplo: "#4CAF50"</summary>
        [MaxLength(7)]
        public string Color { get; set; } = "#ffffff";

        public bool IsDeleted { get; set; }

        // ── Relación inversa para EF Core (N:N con Product) ──────────
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<TagTranslation> Translations { get; set; } = new List<TagTranslation>();
    }
}
