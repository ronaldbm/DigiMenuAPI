using System.ComponentModel.DataAnnotations;
using DigiMenuAPI.Application.Common.Enums;

namespace DigiMenuAPI.Application.DTOs.Update;

public class BranchDiscountUpdateDto
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int BranchId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    public DiscountType DiscountType { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal DefaultValue { get; set; }

    public DiscountAppliesTo AppliesTo { get; set; }

    public bool RequiresApproval { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal? MaxValueForStaff { get; set; }

    public bool IsActive { get; set; }
}
