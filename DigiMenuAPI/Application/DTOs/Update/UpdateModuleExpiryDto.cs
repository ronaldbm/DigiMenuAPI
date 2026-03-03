namespace DigiMenuAPI.Application.DTOs.Update
{
    public record UpdateModuleExpiryDto(
        int CompanyModuleId,
        DateTime? ExpiresAt,
        string? Notes
    );
}
