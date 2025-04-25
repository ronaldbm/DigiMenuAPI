using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.UpdateDTOs
{
    public class ProductUpdateDto
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public required string Label { get; set; }
        public string? Description { get; set; }
        [Required]
        public float Price { get; set; }
        public string? Image { get; set; }
        [Required]
        public int SubcategoryId { get; set; }
        public bool Alive { get; set; }
        public int Position { get; set; }
        public bool IsVisible { get; set; }
    }
}
