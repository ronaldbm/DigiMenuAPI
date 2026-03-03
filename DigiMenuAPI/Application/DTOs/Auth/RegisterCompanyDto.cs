namespace DigiMenuAPI.Application.DTOs.Auth
{
    public record RegisterCompanyDto(
        string CompanyName,
        string Slug,
        string CompanyEmail,
        string? CompanyPhone,
        string? CountryCode,
        string AdminFullName,
        string AdminEmail,
        string AdminPassword
    );
}
