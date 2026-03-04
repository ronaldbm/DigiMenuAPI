using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Traducción del nombre de una categoría a un idioma específico.
    /// Si no existe traducción para el idioma solicitado, el servicio
    /// hace fallback al idioma base definido en Setting.Language de la Branch.
    ///
    /// Índice único: (CategoryId + LanguageCode) → una sola traducción por idioma.
    /// </summary>
    public class CategoryTranslation
    {
        public int Id { get; set; }

        // ── Relación ──────────────────────────────────────────────────
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        // ── Contenido traducido ───────────────────────────────────────
        /// <summary>Código ISO 639-1. Ejemplos: "es", "en", "pt", "fr"</summary>
        [Required, MaxLength(5)]
        public string LanguageCode { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;
    }
}
