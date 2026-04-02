using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Create;

public class AccountCreateDto
{
    [Range(1, int.MaxValue)]
    public int BranchId { get; set; }

    [Required, MaxLength(200)]
    public string ClientIdentifier { get; set; } = null!;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public int? CustomerId { get; set; }
}
