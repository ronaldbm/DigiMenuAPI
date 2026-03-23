using AppCore.Application.Common;
using AppCore.Application.Interfaces;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services
{
    public class BranchPromotionService : IBranchPromotionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly IFileStorageService _fileStorage;
        private readonly ICacheService _cache;
        private const string Container = "promotions";

        public BranchPromotionService(
            ApplicationDbContext context,
            ITenantService tenantService,
            IFileStorageService fileStorage,
            ICacheService cache)
        {
            _context = context;
            _tenantService = tenantService;
            _fileStorage = fileStorage;
            _cache = cache;
        }

        public async Task<OperationResult<List<BranchPromotionReadDto>>> GetByBranch(int branchId)
        {
            await _tenantService.ValidateBranchOwnershipAsync(branchId);

            var promos = await _context.BranchPromotions
                .AsNoTracking()
                .Where(p => p.BranchId == branchId)
                .Include(p => p.BranchProduct)
                    .ThenInclude(bp => bp!.Product)
                        .ThenInclude(prod => prod!.Translations)
                .OrderBy(p => p.DisplayOrder)
                .ThenBy(p => p.CreatedAt)
                .ToListAsync();

            return OperationResult<List<BranchPromotionReadDto>>.Ok(
                promos.Select(MapToDto).ToList());
        }

        public async Task<OperationResult<BranchPromotionReadDto>> GetById(int id)
        {
            var promo = await _context.BranchPromotions
                .AsNoTracking()
                .Include(p => p.BranchProduct)
                    .ThenInclude(bp => bp!.Product)
                        .ThenInclude(prod => prod!.Translations)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (promo is null)
                return OperationResult<BranchPromotionReadDto>.NotFound(
                    "La promoción no fue encontrada.", ErrorKeys.PromotionNotFound);

            await _tenantService.ValidateBranchOwnershipAsync(promo.BranchId);

            return OperationResult<BranchPromotionReadDto>.Ok(MapToDto(promo));
        }

        public async Task<OperationResult<BranchPromotionReadDto>> Create(BranchPromotionCreateDto dto)
        {
            await _tenantService.ValidateBranchOwnershipAsync(dto.BranchId);

            var (dateOk, dateMsg) = ValidateDates(dto.StartDate, dto.EndDate);
            if (!dateOk)
                return OperationResult<BranchPromotionReadDto>.ValidationError(
                    dateMsg!, ErrorKeys.PromotionEndBeforeStart);

            string? imageUrl = null;
            if (dto.PromoImage is not null)
                imageUrl = await _fileStorage.SaveFile(dto.PromoImage, Container);

            var (timeOk, timeMsg) = ValidateTimes(dto.StartTime, dto.EndTime, dto.StartDate, dto.EndDate);
            if (!timeOk)
                return OperationResult<BranchPromotionReadDto>.ValidationError(
                    timeMsg!, ErrorKeys.PromotionEndBeforeStart);

            var promo = new BranchPromotion
            {
                BranchId          = dto.BranchId,
                BranchProductId   = dto.BranchProductId,
                Title             = dto.Title.Trim(),
                Description       = dto.Description?.Trim(),
                Label             = dto.Label?.Trim(),
                PromoImageUrl     = imageUrl,
                StartDate         = dto.StartDate,
                EndDate           = dto.EndDate,
                StartTime         = dto.StartTime,
                EndTime           = dto.EndTime,
                ShowInCarousel    = dto.ShowInCarousel,
                DisplayOrder      = dto.DisplayOrder,
                IsActive          = true,
                PromoObjectFit    = dto.PromoObjectFit ?? "cover",
                PromoObjectPosition = dto.PromoObjectPosition ?? "50% 50%",
            };

            _context.BranchPromotions.Add(promo);
            await _context.SaveChangesAsync();
            await _cache.EvictMenuByBranchAsync(dto.BranchId);

            // Reload with navigation for full DTO
            promo = await _context.BranchPromotions
                .AsNoTracking()
                .Include(p => p.BranchProduct)
                    .ThenInclude(bp => bp!.Product)
                        .ThenInclude(prod => prod!.Translations)
                .FirstAsync(p => p.Id == promo.Id);

            return OperationResult<BranchPromotionReadDto>.Ok(
                MapToDto(promo), "Promoción creada correctamente.");
        }

        public async Task<OperationResult<BranchPromotionReadDto>> Update(BranchPromotionUpdateDto dto)
        {
            var promo = await _context.BranchPromotions
                .FirstOrDefaultAsync(p => p.Id == dto.Id);

            if (promo is null)
                return OperationResult<BranchPromotionReadDto>.NotFound(
                    "La promoción no fue encontrada.", ErrorKeys.PromotionNotFound);

            await _tenantService.ValidateBranchOwnershipAsync(promo.BranchId);

            var (dateOk, dateMsg) = ValidateDates(dto.StartDate, dto.EndDate);
            if (!dateOk)
                return OperationResult<BranchPromotionReadDto>.ValidationError(
                    dateMsg!, ErrorKeys.PromotionEndBeforeStart);

            if (dto.PromoImage is not null)
            {
                if (promo.PromoImageUrl is not null)
                    _fileStorage.DeleteFile(promo.PromoImageUrl, Container);
                promo.PromoImageUrl = await _fileStorage.SaveFile(dto.PromoImage, Container);
            }

            var (timeOk, timeMsg) = ValidateTimes(dto.StartTime, dto.EndTime, dto.StartDate, dto.EndDate);
            if (!timeOk)
                return OperationResult<BranchPromotionReadDto>.ValidationError(
                    timeMsg!, ErrorKeys.PromotionEndBeforeStart);

            promo.BranchProductId = dto.BranchProductId;
            promo.Title           = dto.Title.Trim();
            promo.Description     = dto.Description?.Trim();
            promo.Label           = dto.Label?.Trim();
            promo.StartDate       = dto.StartDate;
            promo.EndDate         = dto.EndDate;
            promo.StartTime       = dto.StartTime;
            promo.EndTime         = dto.EndTime;
            promo.ShowInCarousel  = dto.ShowInCarousel;
            promo.DisplayOrder    = dto.DisplayOrder;
            promo.IsActive        = dto.IsActive;
            if (dto.PromoObjectFit is not null)      promo.PromoObjectFit      = dto.PromoObjectFit;
            if (dto.PromoObjectPosition is not null)  promo.PromoObjectPosition = dto.PromoObjectPosition;

            await _context.SaveChangesAsync();
            await _cache.EvictMenuByBranchAsync(promo.BranchId);

            promo = await _context.BranchPromotions
                .AsNoTracking()
                .Include(p => p.BranchProduct)
                    .ThenInclude(bp => bp!.Product)
                        .ThenInclude(prod => prod!.Translations)
                .FirstAsync(p => p.Id == promo.Id);

            return OperationResult<BranchPromotionReadDto>.Ok(
                MapToDto(promo), "Promoción actualizada correctamente.");
        }

        public async Task<OperationResult<bool>> Reorder(int branchId, List<ReorderItemDto> items)
        {
            await _tenantService.ValidateBranchOwnershipAsync(branchId);

            var ids    = items.Select(x => x.Id).ToList();
            var promos = await _context.BranchPromotions
                .Where(p => p.BranchId == branchId && ids.Contains(p.Id))
                .ToListAsync();

            foreach (var item in items)
            {
                var promo = promos.FirstOrDefault(p => p.Id == item.Id);
                if (promo is not null) promo.DisplayOrder = item.DisplayOrder;
            }

            await _context.SaveChangesAsync();
            await _cache.EvictMenuByBranchAsync(branchId);
            return OperationResult<bool>.Ok(true, "Orden guardado.");
        }

        public async Task<OperationResult<bool>> Delete(int id)
        {
            var promo = await _context.BranchPromotions
                .FirstOrDefaultAsync(p => p.Id == id);

            if (promo is null)
                return OperationResult<bool>.NotFound(
                    "La promoción no fue encontrada.", ErrorKeys.PromotionNotFound);

            await _tenantService.ValidateBranchOwnershipAsync(promo.BranchId);

            if (promo.PromoImageUrl is not null)
                _fileStorage.DeleteFile(promo.PromoImageUrl, Container);

            var branchId = promo.BranchId;
            _context.BranchPromotions.Remove(promo);
            await _context.SaveChangesAsync();
            await _cache.EvictMenuByBranchAsync(branchId);

            return OperationResult<bool>.Ok(true, "Promoción eliminada.");
        }

        // ── Helpers ────────────────────────────────────────────────────

        private static BranchPromotionReadDto MapToDto(BranchPromotion p)
        {
            string? productName = p.BranchProduct?.Product?.Translations?.FirstOrDefault()?.Name;
            return new BranchPromotionReadDto(
                p.Id,
                p.BranchId,
                p.BranchProductId,
                productName,
                p.Title,
                p.Description,
                p.Label,
                p.PromoImageUrl,
                p.StartDate,
                p.EndDate,
                p.StartTime,
                p.EndTime,
                p.ShowInCarousel,
                p.DisplayOrder,
                p.IsActive,
                p.CreatedAt,
                p.PromoObjectFit,
                p.PromoObjectPosition
            );
        }

        private static (bool ok, string? msg) ValidateDates(DateOnly? start, DateOnly? end)
        {
            if (start.HasValue && end.HasValue && end.Value < start.Value)
                return (false, "La fecha de fin debe ser igual o posterior a la fecha de inicio.");
            return (true, null);
        }

        /// <summary>
        /// La hora de fin solo puede ser anterior a la de inicio si las fechas son distintas
        /// (la promoción termina en un día posterior). Si las fechas son iguales o no se
        /// especifican, la hora de fin debe ser >= hora de inicio.
        /// </summary>
        private static (bool ok, string? msg) ValidateTimes(
            TimeOnly? startTime, TimeOnly? endTime,
            DateOnly? startDate, DateOnly? endDate)
        {
            if (!startTime.HasValue || !endTime.HasValue) return (true, null);

            // Solo restringimos cuando es el mismo día (o no se informaron fechas)
            bool sameDay = !startDate.HasValue || !endDate.HasValue || startDate.Value == endDate.Value;
            if (sameDay && endTime.Value < startTime.Value)
                return (false, "La hora de fin debe ser igual o posterior a la de inicio cuando es el mismo día.");

            return (true, null);
        }
    }
}
