namespace DigiMenuAPI.Application.DTOs.Read
{
    public record CompanyInfoReadDto(
        int Id,
        int CompanyId,
        string BusinessName,
        string? Tagline,
        string? LogoUrl,
        string? FaviconUrl,
        string? BackgroundImageUrl
    );
}
