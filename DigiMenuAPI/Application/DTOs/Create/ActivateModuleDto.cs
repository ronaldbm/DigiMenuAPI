namespace DigiMenuAPI.Application.DTOs.Create
{
    public record ActivateModuleDto(
        int CompanyId,
        int PlatformModuleId,
        DateTime? ExpiresAt,
        string? Notes
    );
}
