namespace DigiMenuAPI.Application.DTOs.ReadDTOs
{
    public class CategoryInfoDto
    {
        public int Id { get; set; }
        public required string Label { get; set; }
        public int Position { get; set; }
    }
}
