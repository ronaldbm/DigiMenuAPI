namespace DigiMenuAPI.Application.DTOs.Auth
{
    /// <summary>
    /// DTO para cambio de contraseña del usuario autenticado.
    /// El UserId se obtiene del JWT — no viene en el body.
    /// </summary>
    public record ChangePasswordDto(
        string CurrentPassword,
        string NewPassword
    );
}