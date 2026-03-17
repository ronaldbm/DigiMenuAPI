namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>
    /// Datos de un evento de sucursal devueltos al cliente (admin o menú público).
    /// IsAllDay es true cuando StartTime y EndTime son null.
    /// </summary>
    public record BranchEventReadDto(
        int Id,
        int BranchId,
        string Title,
        string? Description,
        DateOnly EventDate,
        TimeSpan? StartTime,
        TimeSpan? EndTime,
        string? FlyerImageUrl,
        bool ShowPromotionalModal,
        bool IsAllDay,
        bool IsActive,
        DateTime CreatedAt
    );
}
