namespace DigiMenuAPI.Application.DTOs.ReadDTOs
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Label { get; set; } = "";
        public float Price { get; set; }
        public string? Image { get; set; }
        public SubcategoryDto Subcategory { get; set; } = null!;
        public int Position { get; set; }
        public bool IsVisible { get; set; }
    }
}
