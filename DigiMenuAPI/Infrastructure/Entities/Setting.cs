using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    public class Setting : BaseEntity
    {

        [Required, MaxLength(100)]
        public string BusinessName { get; set; } = null!;

        public string? LogoUrl { get; set; }

        [Required, MaxLength(7)]
        public string PrimaryColor { get; set; } = "#000000";

        [Required, MaxLength(7)]
        public string SecondaryColor { get; set; } = "#ffffff";

        [Required, MaxLength(7)]
        public string BackgroundColor { get; set; } = "#ffffff";

        [Required, MaxLength(7)]
        public string TextColor { get; set; } = "#000000";

        public bool ShowProductDetails { get; set; }

        // 1: Grid, 2: List, 3: Compact
        public byte ProductDisplay { get; set; }
    }
}