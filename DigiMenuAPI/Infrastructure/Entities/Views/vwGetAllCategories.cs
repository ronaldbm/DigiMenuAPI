namespace DigiMenuAPI.Infrastructure.Entities.Views
{
    public class vwGetAllCategories
    {
        public int Id { get; set; }
        public required string Label { get; set; }
        public bool Alive { get; set; }
        public int Position { get; set; }
        public bool IsVisible { get; set; }
        public bool HasSubcategory { get; set; }
    }
}
