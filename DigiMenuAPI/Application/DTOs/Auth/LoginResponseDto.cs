namespace DigiMenuAPI.Application.DTOs.Auth
{
    /// <summary>
    /// Respuesta del login y registro.
    ///
    /// MustChangePassword = true indica que el usuario fue creado con contraseña
    /// temporal y debe cambiarla antes de continuar. El frontend debe redirigir
    /// al formulario de cambio de contraseña al detectar este flag.
    /// </summary>
    public record LoginResponseDto(
        string Token,
        int UserId,
        string FullName,
        string Email,
        int CompanyId,
        string CompanyName,
        string CompanySlug,
        int? BranchId,
        string? BranchName,
        byte Role,
        DateTime ExpiresAt,
        bool MustChangePassword,
        string? AdminLang
    );
}