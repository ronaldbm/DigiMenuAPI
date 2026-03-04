namespace DigiMenuAPI.Application.DTOs.Create
{
    public record CompanyModuleCreateDto(
        int CompanyId,
        int PlatformModuleId,
        DateTime? ExpiresAt,
        string? Notes
    );
}
