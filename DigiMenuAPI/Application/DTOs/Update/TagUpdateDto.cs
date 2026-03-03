namespace DigiMenuAPI.Application.DTOs.Update
{
    public record TagUpdateDto(
        int Id,
        string Name,
        string? Color
    );
}
