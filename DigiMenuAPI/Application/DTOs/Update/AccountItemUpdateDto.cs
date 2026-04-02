using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Update;

public class AccountItemUpdateDto
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int AccountId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [MaxLength(300)]
    public string? Notes { get; set; }
}
