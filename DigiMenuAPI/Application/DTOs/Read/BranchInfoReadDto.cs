namespace DigiMenuAPI.Application.DTOs.Read
{
    public record BranchInfoReadDto(
        int Id,
        int BranchId,
        string BusinessName,
        string? Tagline,
        string? LogoUrl,
        string? FaviconUrl,
        string? BackgroundImageUrl
    );
}