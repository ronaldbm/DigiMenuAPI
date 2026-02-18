using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    public class Tag : BaseEntity
    {
        [Required, MaxLength(50)]
        public string Name { get; set; } = null!;

        public bool IsDeleted { get; set; }

        // Relación inversa para EF Core
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}