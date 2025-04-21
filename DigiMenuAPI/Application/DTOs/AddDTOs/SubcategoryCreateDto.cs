using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.AddDTOs
{
    public class SubcategoryCreateDto
    {
        [Required]
        public required string Label { get; set; }
        [Required]
        public int CategoryId { get; set; }
        public required int Position { get; set; }
        public bool IsVisible { get; set; }
    }
}
