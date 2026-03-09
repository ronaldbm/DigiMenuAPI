namespace DigiMenuAPI.Application.DTOs.Create
{
    /// <summary>
    /// DTO para registro de nueva empresa con su primer CompanyAdmin.
    ///
    /// AdminFullName: nombre real del administrador que se registra.
    /// Password: contraseña del CompanyAdmin.
    ///   - Mínimo 8 caracteres, al menos 1 mayúscula y 1 número.
    ///   - El admin define su propia contraseña — MustChangePassword = false.
    ///
    /// MaxBranches / MaxUsers: null = usar los valores del Plan seleccionado.
    /// </summary>
    public record CompanyCreateDto(
        string Name,
        string AdminFullName,  
        string Email,
        string Password,
        string? Phone,
        string? CountryCode,
        int PlanId,
        int? MaxBranches,
        int? MaxUsers,
        string Slug
    );
}