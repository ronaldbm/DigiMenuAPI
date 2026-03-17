using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Create
{
    /// <summary>
    /// Crea o actualiza la traducción de un producto para un idioma específico.
    /// El LanguageCode viaja en la ruta (PUT /products/{id}/translations/{code}),
    /// no en el body.
    /// </summary>
    public record ProductTranslationUpsertDto(
        [Required, MaxLength(150)] string Name,
        [MaxLength(250)] string? ShortDescription,
        string? LongDescription
    );
}
