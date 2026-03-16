using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Update
{
    public record CompanyContactUpdateDto(
        [Required, MaxLength(200)] string Name,
        [MaxLength(200)] string? Email,
        [MaxLength(30)]  string? Phone,
        [MaxLength(10)]  string? CountryCode);
}
