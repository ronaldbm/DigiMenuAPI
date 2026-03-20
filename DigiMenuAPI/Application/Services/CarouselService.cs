using AppCore.Application.Common;
using AppCore.Application.Common.Enums;
using AppCore.Application.Interfaces;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services
{
    public class CarouselService : ICarouselService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;

        public CarouselService(ApplicationDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public async Task<OperationResult<List<CarouselItemDto>>> GetCarouselItems(
            string companySlug, string branchSlug)
        {
            var (branchId, _) = await _tenantService.ResolveBySlugAsync(companySlug, branchSlug);

            if (branchId is null)
                return OperationResult<List<CarouselItemDto>>.NotFound(
                    "Sucursal no encontrada.", ErrorKeys.BranchNotFound);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // Eventos activos con ShowPromotionalModal=true, fecha >= hoy
            var events = await _context.BranchEvents
                .AsNoTracking()
                .Where(e => e.BranchId == branchId.Value
                         && e.IsActive
                         && e.ShowPromotionalModal
                         && e.EndDate >= today.ToDateTime(TimeOnly.MinValue))
                .OrderBy(e => e.EventDate)
                .ThenBy(e => e.StartTime)
                .ToListAsync();

            // Promociones activas que muestran en carrusel y están dentro del rango de fechas
            var promos = await _context.BranchPromotions
                .AsNoTracking()
                .Where(p => p.BranchId == branchId.Value
                         && p.IsActive
                         && p.ShowInCarousel
                         && (p.StartDate == null || p.StartDate <= today)
                         && (p.EndDate == null || p.EndDate >= today))
                .OrderBy(p => p.DisplayOrder)
                .ThenBy(p => p.CreatedAt)
                .ToListAsync();

            // Eventos primero (más urgentes por fecha), luego promos ordenadas
            var items = new List<CarouselItemDto>();

            items.AddRange(events.Select(e => new CarouselItemDto(
                Type:        CarouselItemType.Event,
                SourceId:    e.Id,
                Title:       e.Title,
                Description: e.Description,
                ImageUrl:    e.FlyerImageUrl,
                EventDate:   DateOnly.FromDateTime(e.EventDate),
                StartTime:   e.StartTime,
                EndTime:     e.EndTime,
                IsAllDay:    e.StartTime is null && e.EndTime is null,
                Label:       null,
                DisplayOrder: null
            )));

            items.AddRange(promos.Select(p => new CarouselItemDto(
                Type:        CarouselItemType.Promotion,
                SourceId:    p.Id,
                Title:       p.Title,
                Description: p.Description,
                ImageUrl:    p.PromoImageUrl,
                EventDate:   null,
                StartTime:   p.StartTime?.ToTimeSpan(),
                EndTime:     p.EndTime?.ToTimeSpan(),
                IsAllDay:    p.StartTime is null && p.EndTime is null ? null : false,
                Label:       p.Label,
                DisplayOrder: p.DisplayOrder
            )));

            return OperationResult<List<CarouselItemDto>>.Ok(items);
        }
    }
}
