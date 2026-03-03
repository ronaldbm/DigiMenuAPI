namespace DigiMenuAPI.Application.DTOs.Auth
{
    public record AuthResultDto(
        string Token,
        string FullName,
        string Email,
        int CompanyId,
        string CompanyName,
        string CompanySlug,
        byte Role,
        DateTime ExpiresAt
    );
}
