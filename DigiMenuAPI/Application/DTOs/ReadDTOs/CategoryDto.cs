namespace DigiMenuAPI.Application.DTOs.ReadDTOs
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public required string Label { get; set; }
        public int Position { get; set; }
    }
}
