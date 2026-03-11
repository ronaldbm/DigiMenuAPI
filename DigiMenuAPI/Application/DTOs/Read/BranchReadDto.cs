namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>
    /// Vista completa de una Branch para el panel admin.
    /// Incluye todos los campos editables y timestamps de auditoría.
    /// </summary>
    public record BranchReadDto(
        int Id,
        int CompanyId,
        string Name,
        string Slug,
        string? Address,
        string? Phone,
        string? Email,
        bool IsActive,
        DateTime CreatedAt,
        DateTime? ModifiedAt   
    );

    /// <summary>
    /// Vista reducida para listas, tablas y selects.
    /// </summary>
    public record BranchSummaryDto(
        int Id,
        string Name,
        string Slug,
        bool IsActive
    );
}