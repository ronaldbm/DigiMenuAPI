using AppCore.Application.Common.Enums;

namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>
    /// DTO público unificado para el carrusel del menú.
    /// Mezcla eventos (ShowPromotionalModal=true) y promociones activas.
    /// </summary>
    public record CarouselItemDto(
        CarouselItemType Type,
        int SourceId,
        string Title,
        string? Description,
        string? ImageUrl,
        DateOnly? EventDate,
        TimeSpan? StartTime,
        TimeSpan? EndTime,
        bool? IsAllDay,
        string? Label,
        int? DisplayOrder
    );
}
