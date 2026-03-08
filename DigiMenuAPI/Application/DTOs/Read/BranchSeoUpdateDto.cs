namespace DigiMenuAPI.Application.DTOs.Update
{
    public record BranchSeoUpdateDto(
        int BranchId,
        string? MetaTitle,
        string? MetaDescription,
        string? GoogleAnalyticsId,
        string? FacebookPixelId
    );
}