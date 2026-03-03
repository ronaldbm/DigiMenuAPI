namespace DigiMenuAPI.Application.DTOs.Add
{
    public record CategoryCreateDto(
        string Name,
        int DisplayOrder,
        bool IsVisible
    );
}
