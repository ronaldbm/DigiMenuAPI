namespace DigiMenuAPI.Application.DTOs.Update
{
    public record FooterLinkUpdateDto(
        int Id,
        string Label,
        string Url,
        int? StandardIconId,
        string? CustomSvgContent,
        int DisplayOrder,
        bool IsVisible
    );
}
