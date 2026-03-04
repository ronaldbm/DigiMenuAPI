namespace DigiMenuAPI.Application.DTOs.Read
{
    public record CompanyModuleReadDto(
        int Id,
        int PlatformModuleId,
        string ModuleName,
        string ModuleCode,
        bool IsActive,
        DateTime ActivatedAt,
        DateTime? ExpiresAt
    );
}
