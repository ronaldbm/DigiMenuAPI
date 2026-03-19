using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Create
{
    /// <summary>
    /// Traducción de nombre para Category y Tag dentro del mismo request de creación/actualización.
    /// El LanguageCode viaja en el body junto con el Name.
    /// </summary>
    public class NameTranslationInput
    {
        [Required, MaxLength(10)]
        public string LanguageCode { get; init; } = string.Empty;

        [Required, MaxLength(150)]
        public string Name { get; init; } = string.Empty;
    }
}
