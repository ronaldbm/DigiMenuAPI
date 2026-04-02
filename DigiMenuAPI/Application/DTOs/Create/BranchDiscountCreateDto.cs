using System.ComponentModel.DataAnnotations;
using DigiMenuAPI.Application.Common.Enums;

namespace DigiMenuAPI.Application.DTOs.Create;

public class BranchDiscountCreateDto
{
    [Range(1, int.MaxValue)]
    public int BranchId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    public DiscountType DiscountType { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal DefaultValue { get; set; }

    public DiscountAppliesTo AppliesTo { get; set; }

    public bool RequiresApproval { get; set; } = false;

    [Range(0.01, double.MaxValue)]
    public decimal? MaxValueForStaff { get; set; }
}
