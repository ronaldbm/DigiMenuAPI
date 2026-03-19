using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Create
{
    /// <summary>
    /// DTO para crear una nueva sucursal.
    /// CompanyId se inyecta desde el JWT en el servicio — no viene en el body.
    /// El Slug es opcional: si no se envía, se genera automáticamente desde el Name.
    /// </summary>
    public record BranchCreateDto(
        [Required, MaxLength(100)] string Name,
        [MaxLength(60)] string? Slug,
        [MaxLength(200)] string? Address,
        [MaxLength(20)] string? Phone,
        [MaxLength(150), EmailAddress] string? Email
    );
}