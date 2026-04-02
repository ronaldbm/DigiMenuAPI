using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Create;

public class ApplyDiscountDto
{
    [Range(1, int.MaxValue)]
    public int AccountId { get; set; }

    [Range(1, int.MaxValue)]
    public int BranchDiscountId { get; set; }

    /// <summary>If set, discount applies to this specific item only. If null, applies to the whole account.</summary>
    public int? AccountItemId { get; set; }

    /// <summary>Override the default value from BranchDiscount. If null, uses BranchDiscount.DefaultValue.</summary>
    [Range(0.01, double.MaxValue)]
    public decimal? DiscountValue { get; set; }

    [Required, MaxLength(300)]
    public string Reason { get; set; } = null!;
}
