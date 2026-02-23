using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    public class Tag : BaseEntity
    {
        [Required, MaxLength(50)]
        public string Name { get; set; } = null!;
        [MaxLength(7)]
        public string Color { get; set; } = "#ffffff";
        public bool IsDeleted { get; set; }

        // Relación inversa para EF Core
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}