using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.AddDTOs
{
    public class ProductCreateDto
    {
        [Required]
        public required string Label { get; set; }
        public string? Description { get; set; }
        [Required]
        public float Price { get; set; }
        public string? Image { get; set; }
        [Required]
        public int SubcategoryId { get; set; }
        public int Position { get; set; }
        public bool IsVisible { get; set; }

    }
}
