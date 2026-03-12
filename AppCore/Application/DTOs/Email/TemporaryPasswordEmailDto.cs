namespace AppCore.Application.DTOs.Email
{
    public record TemporaryPasswordEmailDto(
        string ToEmail,
        string FullName,
        string CompanyName,
        string TemporaryPassword,
        string LoginUrl
    );
}
