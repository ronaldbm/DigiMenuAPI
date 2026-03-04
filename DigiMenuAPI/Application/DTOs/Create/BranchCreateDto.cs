namespace DigiMenuAPI.Application.DTOs.Create
{
    public record BranchCreateDto(
        int CompanyId,
        string Name,
        string Slug,
        string? Address,
        string? Phone,
        string? Email
    );
}
