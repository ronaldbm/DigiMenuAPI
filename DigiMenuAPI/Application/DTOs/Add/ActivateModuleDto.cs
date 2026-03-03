namespace DigiMenuAPI.Application.DTOs.Add
{
    public record ActivateModuleDto(
        int CompanyId,
        int PlatformModuleId,
        DateTime? ExpiresAt,
        string? Notes
    );
}
