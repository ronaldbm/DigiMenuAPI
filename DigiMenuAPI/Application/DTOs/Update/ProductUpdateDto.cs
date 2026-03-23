using System.ComponentModel.DataAnnotations;
using DigiMenuAPI.Application.DTOs.Create;

namespace DigiMenuAPI.Application.DTOs.Update
{
    /// <summary>
    /// Id y CompanyId NO se recibe del cliente — Id va en la ruta, CompanyId se inyecta desde el JWT.
    /// Usa [FromForm] por el campo Image. Translations se bindea con campos indexados:
    /// Translations[0].LanguageCode, Translations[0].Name, etc.
    /// </summary>
    public class ProductUpdateDto
    {
        public int Id { get; init; }

        [Range(1, int.MaxValue, ErrorMessage = "CategoryId inválido.")]
        public int CategoryId { get; init; }
        public IFormFile? Image { get; init; }
        public List<int>? TagIds { get; init; }

        [Required, MinLength(1, ErrorMessage = "Se requiere al menos una traducción.")]
        public List<ProductTranslationInput> Translations { get; init; } = [];

        public string? ImageObjectFit { get; init; }
        public string? ImageObjectPosition { get; init; }
    }
}
