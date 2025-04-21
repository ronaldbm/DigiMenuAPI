namespace DigiMenuAPI.Infrastructure.Entities
{
    public class SocialLink
    {
        public int Id { get; set; }
        public required string Icon { get; set; }
        public required string Label { get; set; }
        public string? URL { get; set; }
        public bool IsVisible { get; set; }
    }
}
