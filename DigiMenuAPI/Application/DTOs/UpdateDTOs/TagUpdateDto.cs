namespace DigiMenuAPI.Application.DTOs.UpdateDTOs
{
    public record TagUpdateDto(
        int Id,
        string Name,
        string? Color
    );
}
