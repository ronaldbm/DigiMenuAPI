namespace DigiMenuAPI.Application.DTOs.ReadDTOs;

public record FooterLinkReadDto(
    int Id,
    string Label,
    string Url,
    string SvgContent,
    int DisplayOrder
)
{
    public FooterLinkReadDto() : this(0, string.Empty, string.Empty, string.Empty, 0) { }
}