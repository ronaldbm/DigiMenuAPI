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