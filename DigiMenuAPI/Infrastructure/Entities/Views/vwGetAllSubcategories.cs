namespace DigiMenuAPI.Infrastructure.Entities.Views
{
    public class vwGetAllSubcategories
    {
        public int Id { get; set; }
        public required string Label { get; set; }
        public bool Alive { get; set; }
        public int Position { get; set; }
        public bool IsVisible { get; set; }
        public bool HasProduct { get; set; }

        //CategortDTO 
        public int CategoryId { get; set; }
        public required string CategoryLabel { get; set; }
        public int CategoryPosition { get; set; }
        public bool CategoryIsVisible { get; set; }
    }
}
