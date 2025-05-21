namespace DigiMenuAPI.Application.DTOs.ReadDTOs
{
    public class SubcategoryInfo
    {
        public int Id { get; set; }
        public required string Label { get; set; }
        public int Position { get; set; }
    }

    public class SubcategoryCategoryDto : SubcategoryInfo
    {
        public CategoryInfoDto Category { get; set; } = null!;
    }
}
