using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Create
{
    /// <summary>
    /// CompanyId NO se recibe del cliente — se inyecta desde el JWT en el servicio.
    /// Translations incluye todas las traducciones activas; el servicio las persiste en una sola transacción.
    /// </summary>
    public class TagCreateDto
    {
        [MaxLength(7)]
        public string? Color { get; init; }

        [Required, MinLength(1, ErrorMessage = "Se requiere al menos una traducción.")]
        public List<NameTranslationInput> Translations { get; init; } = [];
    }
}
