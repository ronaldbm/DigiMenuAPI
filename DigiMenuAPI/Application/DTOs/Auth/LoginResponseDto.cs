namespace DigiMenuAPI.Application.DTOs.Auth
{
    public record LoginResponseDto(
        string Token,
        string FullName,
        string Email,
        int CompanyId,
        string CompanyName,
        string CompanySlug,
        int? BranchId,
        string? BranchName,
        byte Role,
        DateTime ExpiresAt
    );
}
