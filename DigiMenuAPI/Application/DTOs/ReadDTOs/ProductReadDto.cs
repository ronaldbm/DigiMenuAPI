using DigiMenuAPI.Application.DTOs.ReadDTOs;

public record ProductReadDto(
    int Id,
    string Name,
    string ShortDescription,
    decimal BasePrice,
    string ImageUrl,
    List<TagReadDto> Tags
)
{
    public ProductReadDto() : this(0, string.Empty, string.Empty, 0, string.Empty, new List<TagReadDto>()) { }
}

public record ProductAdminReadDto(
    int Id,
    string Name,
    string ShortDescription,
    string LongDescription,
    decimal BasePrice,
    decimal OfferPrice,
    string ImageUrl,
    int CategoryId,      
    bool IsVisible,      
    List<TagReadDto> Tags
)
{
    public ProductAdminReadDto() : this(0, string.Empty, string.Empty, string.Empty, 0, 0, string.Empty, 0, true, new List<TagReadDto>()) { }
}