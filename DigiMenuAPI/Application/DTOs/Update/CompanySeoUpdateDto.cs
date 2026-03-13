namespace DigiMenuAPI.Application.DTOs.Update
{
    public record CompanySeoUpdateDto(
        string? MetaTitle,
        string? MetaDescription,
        string? GoogleAnalyticsId,
        string? FacebookPixelId
    );
}
