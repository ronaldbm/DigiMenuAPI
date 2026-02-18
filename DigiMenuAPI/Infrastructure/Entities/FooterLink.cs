using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    public class FooterLink : BaseEntity
    {

        [Required, MaxLength(50)]
        public string Label { get; set; } = null!;

        [Required, MaxLength(500)]
        public string Url { get; set; } = null!;

        public int? StandardIconId { get; set; }
        public StandardIcon? StandardIcon { get; set; }

        public string? CustomSvgContent { get; set; }

        public int DisplayOrder { get; set; }
        public bool IsVisible { get; set; }
        public bool IsDeleted { get; set; }
    }
}