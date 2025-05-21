namespace DigiMenuAPI.Application.DTOs.ReadDTOs
{
    public class SocialLinkDto
    {
        public int Id { get; set; }
        public required string Icon { get; set; }
        public required string Label { get; set; }
        public string? Url { get; set; }
        public bool IsVisible { get; set; }
    }
}
