using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Create;

public class AccountSplitCreateDto
{
    [Range(1, int.MaxValue)]
    public int AccountId { get; set; }

    [Required, MaxLength(100)]
    public string SplitName { get; set; } = null!;

    [Required, MinLength(1)]
    public List<AccountSplitItemInputDto> Items { get; set; } = new();
}
