namespace DigiMenuAPI.Application.DTOs.ReadDTOs;

public record MenuStoreDto(
    string BusinessName,
    string? LogoUrl,
    string PrimaryColor,
    string SecondaryColor,
    string BackgroundColor,
    string TextColor,
    byte ProductDisplay,
    List<CategoryReadDto> Categories,
    List<FooterLinkReadDto> FooterLinks
)
{
    public MenuStoreDto() : this(
        string.Empty, null, "#000000", "#000000", "#FFFFFF", "#000000", 1, 
        new List<CategoryReadDto>(), new List<FooterLinkReadDto>()
    ) { }
}