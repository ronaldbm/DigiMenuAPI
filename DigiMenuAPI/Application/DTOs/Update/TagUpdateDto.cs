using System.ComponentModel.DataAnnotations;
using DigiMenuAPI.Application.DTOs.Create;

namespace DigiMenuAPI.Application.DTOs.Update
{
    public class TagUpdateDto
    {
        public int Id { get; init; }
        [MaxLength(7)]
        public string? Color { get; init; }

        [Required, MinLength(1, ErrorMessage = "Se requiere al menos una traducción.")]
        public List<NameTranslationInput> Translations { get; init; } = [];
    }
}
