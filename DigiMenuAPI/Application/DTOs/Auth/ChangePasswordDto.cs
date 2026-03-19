using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Auth
{
    /// <summary>
    /// DTO para cambio de contraseña del usuario autenticado.
    /// El UserId se obtiene del JWT — no viene en el body.
    /// </summary>
    public record ChangePasswordDto(
        [Required, MaxLength(100)] string CurrentPassword,
        [Required, MinLength(8), MaxLength(100)] string NewPassword
    );
}