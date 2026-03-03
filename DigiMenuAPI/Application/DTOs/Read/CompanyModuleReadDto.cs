namespace DigiMenuAPI.Application.DTOs.Read
{
    public record CompanyModuleReadDto(
        int Id,
        int CompanyId,
        string CompanyName,
        int PlatformModuleId,
        string ModuleCode,
        string ModuleName,
        bool IsActive,
        DateTime ActivatedAt,
        DateTime? ExpiresAt,
        bool IsExpired,
        string? Notes
    );
}
