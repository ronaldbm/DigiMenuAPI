namespace DigiMenuAPI.Application.DTOs.Update
{
    public record PlanUpdateDto(
        int Id,
        string Name,
        string? Description,
        decimal MonthlyPrice,
        decimal? AnnualPrice,
        int MaxBranches,
        int MaxUsers,
        bool IsPublic,
        bool IsActive,
        int DisplayOrder
    );
}
