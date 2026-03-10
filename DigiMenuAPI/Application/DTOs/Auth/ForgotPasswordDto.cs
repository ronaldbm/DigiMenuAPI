// Application/DTOs/Auth/ForgotPasswordDto.cs
namespace DigiMenuAPI.Application.DTOs.Auth
{
    /// <summary>
    /// Solicitud de recuperación de contraseña.
    /// Solo requiere el email — endpoint público, sin JWT.
    /// </summary>
    public record ForgotPasswordDto(string Email);
}