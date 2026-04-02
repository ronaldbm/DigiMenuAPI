using System.ComponentModel.DataAnnotations;
using DigiMenuAPI.Application.Common.Enums;

namespace DigiMenuAPI.Application.DTOs.Update;

public class AccountStatusUpdateDto
{
    [Range(1, int.MaxValue)]
    public int AccountId { get; set; }

    public AccountStatus Status { get; set; }
}
