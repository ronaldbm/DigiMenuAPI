using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Create
{
    /// <summary>
    /// Traducción completa de un producto dentro del mismo request de creación/actualización.
    /// Se bindea desde form-data con campos indexados: Translations[0].LanguageCode, etc.
    /// </summary>
    public class ProductTranslationInput
    {
        [Required, MaxLength(10)]
        public string LanguageCode { get; init; } = string.Empty;

        [Required, MaxLength(150)]
        public string Name { get; init; } = string.Empty;

        [MaxLength(250)]
        public string? ShortDescription { get; init; }

        [MaxLength(2000)]
        public string? LongDescription { get; init; }
    }
}
