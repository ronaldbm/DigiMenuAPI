using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.AddDTOs
{
    public class CategoryCreateDto
    {
        [Required]
        public required string Label { get; set; }
        public int Position { get; set; }
        public bool IsVisible { get; set; }

    }
}
