namespace AppCore.Application.DTOs.Email
{
    public record WelcomeEmailDto(
        string ToEmail,
        string AdminFullName,
        string CompanyName,
        string CompanySlug,
        string LoginUrl
    );
}
