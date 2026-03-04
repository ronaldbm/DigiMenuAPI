using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Traducción del nombre de un tag a un idioma específico.
    /// Los tags viven en el catálogo global (Company), por lo que su traducción
    /// aplica a todas las Branches que usen ese tag en sus productos.
    ///
    /// Si no existe traducción para el idioma solicitado, el servicio
    /// hace fallback al idioma base definido en Setting.Language de la Branch.
    ///
    /// Índice único: (TagId + LanguageCode) → una sola traducción por idioma.
    /// </summary>
    public class TagTranslation
    {
        public int Id { get; set; }

        // ── Relación ──────────────────────────────────────────────────
        public int TagId { get; set; }
        public Tag Tag { get; set; } = null!;

        // ── Contenido traducido ───────────────────────────────────────
        /// <summary>Código ISO 639-1. Ejemplos: "es", "en", "pt", "fr"</summary>
        [Required, MaxLength(5)]
        public string LanguageCode { get; set; } = null!;

        [Required, MaxLength(50)]
        public string Name { get; set; } = null!;
    }
}
