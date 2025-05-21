namespace DigiMenuAPI.Infrastructure.Entities.Views
{
    public class vwCategoryVisibleList
    {
        public int CategoryId { get; set; }
        public required string CategoryLabel { get; set; }
        public int CategoryPosition { get; set; }
    }
}
