namespace DigiMenuAPI.Application.DTOs.Update
{
    public record BranchUpdateDto(
        int Id,
        string Name,
        string Slug,
        string? Address,
        string? Phone,
        string? Email,
        bool IsActive
    );
}
