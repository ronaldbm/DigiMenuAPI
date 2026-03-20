using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Update
{
    /// <summary>
    /// Edición de datos básicos de una Branch.
    /// IsActive se maneja por separado con PATCH /toggle-active
    /// para evitar ambigüedad entre edición y cambio de estado.
    /// </summary>
    public record BranchUpdateDto(
        int Id,
        [Required, MaxLength(100)] string Name,
        [MaxLength(200)] string? Address,
        [MaxLength(20)] string? Phone,
        [MaxLength(150), EmailAddress] string? Email,
        decimal? Latitude,
        decimal? Longitude
    );
}