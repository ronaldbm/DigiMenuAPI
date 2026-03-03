namespace DigiMenuAPI.Application.DTOs.Update
{
    public record CompanyUpdateDto(
        int Id,
        string Name,
        string Email,
        string? Phone,
        string? CountryCode,
        bool IsActive
    );
}
