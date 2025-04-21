using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public required string Label { get; set; }
        public string? Description { get; set; }
        public float Price { get; set; }
        public string? ImagePath { get; set; }
        public int SubcategoryId { get; set; }
        public Subcategory Subcategory { get; set; } = null!;
        public bool Alive { get; set; }
        public int Position { get; set; }
        public bool IsVisible { get; set; }
    }
}
