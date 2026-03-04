namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>
    /// Vista de un usuario. Nunca expone PasswordHash.
    /// </summary>
    public record AppUserReadDto(
        int Id,
        int CompanyId,
        string CompanyName,
        int? BranchId,
        string? BranchName,
        string FullName,
        string Email,
        byte Role,
        bool IsActive,
        DateTime? LastLoginAt,
        DateTime CreatedAt
    );

    /// <summary>Vista reducida para listas.</summary>
    public record AppUserSummaryDto(
        int Id,
        string FullName,
        string Email,
        byte Role,
        bool IsActive,
        string? BranchName
    );
}