namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>Datos de una promoción devueltos al panel admin.</summary>
    public record BranchPromotionReadDto(
        int Id,
        int BranchId,
        int? BranchProductId,
        string? ProductName,
        string Title,
        string? Description,
        string? Label,
        string? PromoImageUrl,
        DateOnly? StartDate,
        DateOnly? EndDate,
        TimeOnly? StartTime,
        TimeOnly? EndTime,
        bool ShowInCarousel,
        int DisplayOrder,
        bool IsActive,
        DateTime CreatedAt,
        string PromoObjectFit,
        string PromoObjectPosition
    );
}
