namespace DigiMenuAPI.Application.DTOs.AddDTOs
{
    public record CategoryCreateDto(
        string Name,
        int DisplayOrder,
        bool IsVisible
    );
}
