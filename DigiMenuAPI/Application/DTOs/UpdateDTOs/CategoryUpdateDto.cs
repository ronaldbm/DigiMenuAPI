using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.UpdateDTOs
{
    public class CategoryUpdateDto
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public required string Label { get; set; }
        public bool Alive { get; set; }
        [Required]
        public int Position { get; set; }
        public bool IsVisible { get; set; }
    }
}
