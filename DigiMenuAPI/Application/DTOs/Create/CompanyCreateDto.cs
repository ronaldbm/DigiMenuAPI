using System.ComponentModel.DataAnnotations;

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
        [Required, MaxLength(100)] string Name,
        [Required, MaxLength(100)] string AdminFullName,
        [Required, MaxLength(150), EmailAddress] string Email,
        [Required, MinLength(8), MaxLength(100)] string Password,
        [MaxLength(20)] string? Phone,
        [MaxLength(3)] string? CountryCode,
        int PlanId,
        int? MaxBranches,
        int? MaxUsers,
        [Required, MaxLength(60)] string Slug
    );
}