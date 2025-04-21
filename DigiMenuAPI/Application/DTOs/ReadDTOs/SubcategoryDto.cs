namespace DigiMenuAPI.Application.DTOs.ReadDTOs
{
    public class SubcategoryDto
    {
        public int Id { get; set; }
        public  required string Label { get; set; }
        public int Position { get; set; }
        public required CategoryDto Category { get; set; }
    }
}
