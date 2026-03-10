namespace DigiMenuAPI.Application.DTOs.Email
{
    public record ForgotPasswordEmailDto(
        string ToEmail,
        string FullName,
        string ResetUrl,
        DateTime ExpiresAt
    );
}