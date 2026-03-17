using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Create
{
    /// <summary>
    /// Crea o actualiza la traducción de nombre para categorías y tags.
    /// El LanguageCode viaja en la ruta (PUT /{entity}/{id}/translations/{code}),
    /// no en el body.
    /// </summary>
    public record NameTranslationUpsertDto(
        [Required, MaxLength(150)] string Name
    );
}
