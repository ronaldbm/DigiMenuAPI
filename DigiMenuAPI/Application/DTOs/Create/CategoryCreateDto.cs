namespace DigiMenuAPI.Application.DTOs.Create
{
    public record CategoryCreateDto(
        int CompanyId,
        string Name,
        int DisplayOrder,
        bool IsVisible
    );
}
