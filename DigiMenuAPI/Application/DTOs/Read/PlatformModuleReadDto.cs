namespace DigiMenuAPI.Application.DTOs.Read
{
    public record PlatformModuleReadDto(
        int Id,
        string Code,
        string Name,
        string? Description,
        bool IsActive,
        int DisplayOrder
    );
}
