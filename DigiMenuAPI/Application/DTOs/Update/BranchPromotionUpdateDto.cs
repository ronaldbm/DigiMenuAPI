using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Update
{
    public class BranchPromotionUpdateDto
    {
        [Range(1, int.MaxValue)]
        public int Id { get; set; }

        [Range(1, int.MaxValue)]
        public int BranchId { get; set; }

        public int? BranchProductId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = null!;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? Label { get; set; }

        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }

        public bool ShowInCarousel { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }

        public IFormFile? PromoImage { get; set; }

        public string? PromoObjectFit { get; set; }
        public string? PromoObjectPosition { get; set; }
    }
}
