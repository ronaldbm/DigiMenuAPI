namespace DigiMenuAPI.Application.DTOs.Create
{
    public record PlanCreateDto(
        string Code,
        string Name,
        string? Description,
        decimal MonthlyPrice,
        decimal? AnnualPrice,
        int MaxBranches,
        int MaxUsers,
        bool IsPublic,
        int DisplayOrder
    );
}
