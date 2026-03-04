using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Traducción del contenido textual de un producto a un idioma específico.
    /// Vive en el catálogo global (Product), por lo que aplica a todas las
    /// Branches que tengan activo ese producto via BranchProduct.
    ///
    /// Si no existe traducción para el idioma solicitado, el servicio
    /// hace fallback al idioma base definido en Setting.Language de la Branch.
    ///
    /// Índice único: (ProductId + LanguageCode) → una sola traducción por idioma.
    /// </summary>
    public class ProductTranslation
    {
        public int Id { get; set; }

        // ── Relación ──────────────────────────────────────────────────
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        // ── Contenido traducido ───────────────────────────────────────
        /// <summary>Código ISO 639-1. Ejemplos: "es", "en", "pt", "fr"</summary>
        [Required, MaxLength(5)]
        public string LanguageCode { get; set; } = null!;

        [Required, MaxLength(150)]
        public string Name { get; set; } = null!;

        public string? ShortDescription { get; set; }
        public string? LongDescription { get; set; }
    }
}
