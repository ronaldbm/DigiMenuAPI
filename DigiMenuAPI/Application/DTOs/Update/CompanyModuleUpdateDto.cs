namespace DigiMenuAPI.Application.DTOs.Update
{
    public record CompanyModuleUpdateDto(
        int Id,
        bool IsActive,
        DateTime? ExpiresAt,
        string? Notes
    );
}
