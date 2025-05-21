namespace DigiMenuAPI.Infrastructure.Entities.Views
{
    public class vwProductVisibleList
    {
        public int ProductId { get; set; }
        public required string ProductLabel { get; set; }
        public string? ProductImage { get; set; }
        public double ProductPrice { get; set; }
        public int ProductPosition { get; set; }

        public int SubcategoryId { get; set; }
        public required string SubcategoryLabel { get; set; }
        public int SubcategoryPosition { get; set; }

        public int CategoryId { get; set; }
        public required string CategoryLabel { get; set; }
        public int CategoryPosition { get; set; }
    }
}
