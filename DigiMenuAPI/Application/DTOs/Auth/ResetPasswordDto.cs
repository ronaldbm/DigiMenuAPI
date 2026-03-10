namespace DigiMenuAPI.Application.DTOs.Auth
{
    /// <summary>
    /// Aplicar nueva contraseña usando el token del email.
    /// El token se valida contra PasswordResetRequest en DB.
    /// </summary>
    public record ResetPasswordDto(
        string Token,
        string NewPassword
    );
}