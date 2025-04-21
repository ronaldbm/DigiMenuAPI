using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    public class Subcategory
    {
        public int Id { get; set; }
        public required string Label { get; set; }
        public int CategoryId { get; set; }
        // Propiedad de navegación hacia Category
        public Category Category { get; set; } = null!;  // Relación con la entidad Category
        public bool Alive { get; set; }
        public int Position { get; set; }
        public bool IsVisible { get; set; }


        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
