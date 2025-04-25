namespace DigiMenuAPI.Application.DTOs.ReadDTOs
{
    public class CategorySelectInformation
    {
        public int Id { get; set; }
        public required string Label { get; set; }
        public int Position { get; set; }
        public List<SubcategoryInfo> Subcategory { get; set; } = new List<SubcategoryInfo>();
    }
}
