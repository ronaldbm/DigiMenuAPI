using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Create;

public class AccountItemCreateDto
{
    [Range(1, int.MaxValue)]
    public int AccountId { get; set; }

    [Range(1, int.MaxValue)]
    public int BranchProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    [MaxLength(300)]
    public string? Notes { get; set; }
}
