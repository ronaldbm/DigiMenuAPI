namespace DigiMenuAPI.Application.DTOs.ReadDTOs
{
    public class MenuProductsDto
    {
        public int Id { get; set; }
        public string Label { get; set; } = "";
        public double Price { get; set; }
        public string? Image { get; set; }
        public SubcategoryCategoryDto Subcategory { get; set; } = null!;
    }
}
