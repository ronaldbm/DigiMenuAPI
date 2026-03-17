using System.ComponentModel.DataAnnotations;

namespace AppCore.Domain.Entities
{
    /// <summary>
    /// Catálogo global de idiomas que la plataforma soporta.
    /// Administrado a nivel de plataforma (SuperAdmin).
    /// Para agregar un nuevo idioma en el futuro basta con insertar un registro aquí.
    /// </summary>
    public class SupportedLanguage
    {
        /// <summary>Código ISO 639-1 (ej: "es", "en", "fr", "pt"). Clave primaria.</summary>
        [Key, MaxLength(5)]
        public string Code { get; set; } = null!;

        /// <summary>Nombre del idioma en su propio idioma (ej: "Español", "English").</summary>
        [Required, MaxLength(50)]
        public string Name { get; set; } = null!;

        /// <summary>Emoji de la bandera representativa (ej: "🇪🇸").</summary>
        [MaxLength(10)]
        public string Flag { get; set; } = null!;

        /// <summary>Orden de visualización en la UI.</summary>
        public int DisplayOrder { get; set; }

        /// <summary>Permite deshabilitar un idioma sin eliminarlo del catálogo.</summary>
        public bool IsActive { get; set; } = true;

        public ICollection<CompanyLanguage> CompanyLanguages { get; set; } = new List<CompanyLanguage>();
    }
}
