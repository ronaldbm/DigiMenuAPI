using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Create
{
    /// <summary>
    /// CompanyId NO se recibe del cliente — se inyecta desde el JWT en el servicio.
    /// Usa [FromForm] por el campo Image. Translations se bindea con campos indexados:
    /// Translations[0].LanguageCode, Translations[0].Name, etc.
    /// </summary>
    public class ProductCreateDto
    {
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
