namespace DigiMenuAPI.Application.DTOs.Read
{
    public record BranchSeoReadDto(
        int Id,
        int BranchId,
        string? MetaTitle,
        string? MetaDescription,
        string? GoogleAnalyticsId,
        string? FacebookPixelId
    );
}