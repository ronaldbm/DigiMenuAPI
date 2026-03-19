using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Auth
{
    public record LoginRequestDto(
        [Required, MaxLength(150), EmailAddress] string Email,
        [Required, MaxLength(100)] string Password
    );
}
