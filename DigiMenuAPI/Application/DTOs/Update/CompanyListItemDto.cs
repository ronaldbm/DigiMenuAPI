namespace DigiMenuAPI.Application.DTOs.Update
{
    public record CompanyListItemDto(
        int Id,
        string Name,
        string Slug,
        string Email,
        bool IsActive,
        int ActiveModulesCount,
        DateTime CreatedAt
    );
}
