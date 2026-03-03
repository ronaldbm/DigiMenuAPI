namespace DigiMenuAPI.Application.DTOs.Read
{
    public record CompanyReadDto(
        int Id,
        string Name,
        string Slug,
        string Email,
        string? Phone,
        string? CountryCode,
        bool IsActive,
        DateTime CreatedAt,
        List<CompanyModuleReadDto> ActiveModules
    );
}
