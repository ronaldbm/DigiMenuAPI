using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Update;

public class AuthorizeDiscountDto
{
    [Range(1, int.MaxValue)]
    public int AccountDiscountId { get; set; }

    public bool Approved { get; set; }

    [MaxLength(300)]
    public string? Notes { get; set; }
}
