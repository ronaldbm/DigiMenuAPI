using System.ComponentModel.DataAnnotations;

namespace AppCore.Domain.Entities
{
    /// <summary>
    /// Idiomas habilitados para una Company específica.
    /// Define qué idiomas puede ver el cliente en el menú público
    /// y para cuáles se deben mantener traducciones.
    ///
    /// Relación N:N entre Company y SupportedLanguage.
    /// Unique constraint: (CompanyId, LanguageCode).
    /// </summary>
    public class CompanyLanguage : BaseEntity
    {
        // ── Multi-Tenant ─────────────────────────────────────────────
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        // ── Idioma ───────────────────────────────────────────────────
        [Required, MaxLength(5)]
        public string LanguageCode { get; set; } = null!;
        public SupportedLanguage Language { get; set; } = null!;

        /// <summary>
        /// Idioma principal de la empresa.
        /// Solo uno puede ser default por Company.
        /// Es el idioma de fallback cuando no existe traducción para otro idioma.
        /// </summary>
        public bool IsDefault { get; set; }
    }
}
