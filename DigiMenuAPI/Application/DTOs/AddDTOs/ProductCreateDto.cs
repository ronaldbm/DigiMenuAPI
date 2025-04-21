using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.AddDTOs
{
    public class ProductCreateDto
    {
        [Required]
        public required string Label { get; set; }
        [Required]
        public float Price { get; set; }
        public string? Image { get; set; }
        [Required]
        public int SubcategoryId { get; set; }
        public required int Position { get; set; }
        public bool IsVisible { get; set; }

    }
}
