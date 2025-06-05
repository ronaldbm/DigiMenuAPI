namespace DigiMenuAPI.Infrastructure.Entities.Views
{
    public class vwGetAllProducts
    {
        public int Id { get; set; }
        public required string Label { get; set; }
        public double Price { get; set; }
        public string? ImagePath { get; set; }
        public int Position { get; set; }
        public bool IsVisible { get; set; }

        //SubcategoryDTO 
        public int SubcategoryId { get; set; }
        public required string SubcategoryLabel { get; set; }
        public int SubcategoryPosition { get; set; }
        public bool SubcategoryIsVisible { get; set; }

        //CategoryDTO 
        public int CategoryId { get; set; }
        public required string CategoryLabel { get; set; }
        public int CategoryPosition { get; set; }
        public bool CategoryIsVisible { get; set; }
    }
}
