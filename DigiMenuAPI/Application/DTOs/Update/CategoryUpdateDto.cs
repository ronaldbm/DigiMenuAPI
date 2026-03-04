namespace DigiMenuAPI.Application.DTOs.Update
{
    public record CategoryUpdateDto(
        int Id,
        string Name,
        int DisplayOrder,
        bool IsVisible
    );
}
