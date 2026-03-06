namespace DigiMenuAPI.Application.DTOs.Create
{
    public record TagCreateDto(
        int CompanyId,
        string Name,
        string? Color
    );
}
