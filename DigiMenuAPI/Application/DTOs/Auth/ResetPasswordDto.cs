using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Auth
{
    /// <summary>
    /// Aplicar nueva contraseña usando el token del email.
    /// El token se valida contra PasswordResetRequest en DB.
    /// </summary>
    public record ResetPasswordDto(
        [Required] string Token,
        [Required, MinLength(8), MaxLength(100)] string NewPassword
    );
}