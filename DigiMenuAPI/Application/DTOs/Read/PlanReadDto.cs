namespace DigiMenuAPI.Application.DTOs.Read
{
    public record PlanReadDto(
        int Id,
        string Code,
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