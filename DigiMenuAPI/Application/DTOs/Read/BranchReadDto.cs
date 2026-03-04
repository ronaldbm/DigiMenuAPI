namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>Vista completa de una Branch para el panel admin.</summary>
    public record BranchReadDto(
        int Id,
        int CompanyId,
        string Name,
        string Slug,
        string? Address,
        string? Phone,
        string? Email,
        bool IsActive,
        DateTime CreatedAt
    );

    /// <summary>Vista reducida para listas y selects.</summary>
    public record BranchSummaryDto(
        int Id,
        string Name,
        string Slug,
        bool IsActive
    );
}