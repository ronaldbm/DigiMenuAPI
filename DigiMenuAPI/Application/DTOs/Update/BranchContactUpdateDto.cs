using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Update
{
    public record BranchContactUpdateDto(
        [Required, MaxLength(200)] string Name,
        [MaxLength(400)] string? Address,
        [MaxLength(30)]  string? Phone,
        [MaxLength(200)] string? Email);
}
