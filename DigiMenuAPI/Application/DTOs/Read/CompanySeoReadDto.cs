namespace DigiMenuAPI.Application.DTOs.Read
{
    public record CompanySeoReadDto(
        int Id,
        int CompanyId,
        string? MetaTitle,
        string? MetaDescription,
        string? GoogleAnalyticsId,
        string? FacebookPixelId
    );
}
