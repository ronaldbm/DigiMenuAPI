using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Create;

public class AccountSplitItemInputDto
{
    [Range(1, int.MaxValue)]
    public int AccountItemId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Quantity { get; set; }
}
