namespace DigiMenuAPI.Application.DTOs.UpdateDTOs
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
