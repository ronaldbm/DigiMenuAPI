namespace DigiMenuAPI.Infrastructure.Entities
{
    public class Category
    {
        public int Id { get; set; }
        public required string Label { get; set; }
        public bool Alive { get; set; }
        public int Position { get; set; }
        public bool IsVisible { get; set; }
        public ICollection<Subcategory> Subcategories { get; set; } = new List<Subcategory>();  // Relación uno a muchos con Subcategory
    }
}
