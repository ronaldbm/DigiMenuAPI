namespace DigiMenuAPI.Application.DTOs.Add
{
    public record TagCreateDto(
        int CompanyId,
        string Name,
        string? Color
    );
}
