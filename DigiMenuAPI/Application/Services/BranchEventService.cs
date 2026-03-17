using AppCore.Application.Common;
using AppCore.Application.Interfaces;
using AppCore.Domain.Entities;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services
{
    public class BranchEventService : IBranchEventService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly IFileStorageService _fileStorage;
        private readonly ICacheService _cache;

        public BranchEventService(
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

        private const string Container = "events";

        // ── Admin ──────────────────────────────────────────────────────

        public async Task<OperationResult<List<BranchEventReadDto>>> GetEvents(int branchId)
        {
            await _tenantService.ValidateBranchOwnershipAsync(branchId);

            var events = await _context.BranchEvents
                .AsNoTracking()
                .Where(e => e.BranchId == branchId)
                .OrderBy(e => e.EventDate)
                .ThenBy(e => e.StartTime)
                .ToListAsync();

            return OperationResult<List<BranchEventReadDto>>.Ok(
                events.Select(MapToDto).ToList());
        }

        public async Task<OperationResult<BranchEventReadDto>> GetById(int id)
        {
            var ev = await _context.BranchEvents.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev is null)
                return OperationResult<BranchEventReadDto>.NotFound(
                    "El evento no fue encontrado.", ErrorKeys.EventNotFound);

            await _tenantService.ValidateBranchOwnershipAsync(ev.BranchId);

            return OperationResult<BranchEventReadDto>.Ok(MapToDto(ev));
        }

        public async Task<OperationResult<BranchEventReadDto>> Create(BranchEventCreateDto dto)
        {
            await _tenantService.ValidateBranchOwnershipAsync(dto.BranchId);

            var today = DateTime.UtcNow.Date;
            if (dto.EventDate.ToDateTime(TimeOnly.MinValue) < today)
                return OperationResult<BranchEventReadDto>.ValidationError(
                    "No se pueden crear eventos en fechas pasadas.", ErrorKeys.EventPastDate);

            var (timeOk, timeMsg, timeKey) = ValidateTimes(dto.StartTime, dto.EndTime);
            if (!timeOk)
                return OperationResult<BranchEventReadDto>.ValidationError(timeMsg!, timeKey!);

            string? flyerUrl = null;
            if (dto.FlyerImage is not null)
                flyerUrl = await _fileStorage.SaveFile(dto.FlyerImage, Container);

            var ev = new BranchEvent
            {
                BranchId             = dto.BranchId,
                Title                = dto.Title.Trim(),
                Description          = dto.Description?.Trim(),
                EventDate            = dto.EventDate.ToDateTime(TimeOnly.MinValue),
                StartTime            = dto.StartTime,
                EndTime              = dto.EndTime,
                FlyerImageUrl        = flyerUrl,
                ShowPromotionalModal = dto.ShowPromotionalModal,
                IsActive             = true,
            };

            _context.BranchEvents.Add(ev);
            await _context.SaveChangesAsync();
            await _cache.EvictMenuByBranchAsync(dto.BranchId);

            return OperationResult<BranchEventReadDto>.Ok(MapToDto(ev),
                "Evento creado correctamente.");
        }

        public async Task<OperationResult<BranchEventReadDto>> Update(BranchEventUpdateDto dto)
        {
            var ev = await _context.BranchEvents
                .FirstOrDefaultAsync(e => e.Id == dto.Id);

            if (ev is null)
                return OperationResult<BranchEventReadDto>.NotFound(
                    "El evento no fue encontrado.", ErrorKeys.EventNotFound);

            await _tenantService.ValidateBranchOwnershipAsync(ev.BranchId);

            var today = DateTime.UtcNow.Date;
            if (dto.EventDate.ToDateTime(TimeOnly.MinValue) < today)
                return OperationResult<BranchEventReadDto>.ValidationError(
                    "No se pueden programar eventos en fechas pasadas.", ErrorKeys.EventPastDate);

            var (timeOk, timeMsg, timeKey) = ValidateTimes(dto.StartTime, dto.EndTime);
            if (!timeOk)
                return OperationResult<BranchEventReadDto>.ValidationError(timeMsg!, timeKey!);

            // Reemplazar flyer si se sube uno nuevo
            if (dto.FlyerImage is not null)
            {
                if (ev.FlyerImageUrl is not null)
                    _fileStorage.DeleteFile(ev.FlyerImageUrl, Container);

                ev.FlyerImageUrl = await _fileStorage.SaveFile(dto.FlyerImage, Container);
            }

            ev.Title                = dto.Title.Trim();
            ev.Description          = dto.Description?.Trim();
            ev.EventDate            = dto.EventDate.ToDateTime(TimeOnly.MinValue);
            ev.StartTime            = dto.StartTime;
            ev.EndTime              = dto.EndTime;
            ev.ShowPromotionalModal = dto.ShowPromotionalModal;
            ev.IsActive             = dto.IsActive;

            await _context.SaveChangesAsync();
            await _cache.EvictMenuByBranchAsync(ev.BranchId);

            return OperationResult<BranchEventReadDto>.Ok(MapToDto(ev),
                "Evento actualizado correctamente.");
        }

        public async Task<OperationResult<bool>> Delete(int id)
        {
            var ev = await _context.BranchEvents
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev is null)
                return OperationResult<bool>.NotFound(
                    "El evento no fue encontrado.", ErrorKeys.EventNotFound);

            await _tenantService.ValidateBranchOwnershipAsync(ev.BranchId);

            if (ev.FlyerImageUrl is not null)
                _fileStorage.DeleteFile(ev.FlyerImageUrl, Container);

            var branchId = ev.BranchId;
            _context.BranchEvents.Remove(ev);
            await _context.SaveChangesAsync();
            await _cache.EvictMenuByBranchAsync(branchId);

            return OperationResult<bool>.Ok(true, "Evento eliminado.");
        }

        // ── Público ────────────────────────────────────────────────────

        public async Task<OperationResult<List<BranchEventReadDto>>> GetUpcomingEvents(
            string companySlug, string branchSlug)
        {
            var (branchId, _) = await _tenantService.ResolveBySlugAsync(companySlug, branchSlug);

            if (branchId is null)
                return OperationResult<List<BranchEventReadDto>>.NotFound(
                    "Sucursal no encontrada.", ErrorKeys.BranchNotFound);

            var today = DateTime.UtcNow.Date;

            var events = await _context.BranchEvents
                .AsNoTracking()
                .Where(e => e.BranchId == branchId.Value
                         && e.IsActive
                         && e.EventDate >= today)
                .OrderBy(e => e.EventDate)
                .ThenBy(e => e.StartTime)
                .ToListAsync();

            return OperationResult<List<BranchEventReadDto>>.Ok(
                events.Select(MapToDto).ToList());
        }

        public async Task<OperationResult<BranchEventReadDto?>> GetNextAnnouncement(
            string companySlug, string branchSlug)
        {
            var (branchId, _) = await _tenantService.ResolveBySlugAsync(companySlug, branchSlug);

            if (branchId is null)
                return OperationResult<BranchEventReadDto?>.NotFound(
                    "Sucursal no encontrada.", ErrorKeys.BranchNotFound);

            var today = DateTime.UtcNow.Date;

            var ev = await _context.BranchEvents
                .AsNoTracking()
                .Where(e => e.BranchId == branchId.Value
                         && e.IsActive
                         && e.ShowPromotionalModal
                         && e.EventDate >= today)
                .OrderBy(e => e.EventDate)
                .ThenBy(e => e.StartTime)
                .FirstOrDefaultAsync();

            return OperationResult<BranchEventReadDto?>.Ok(ev is null ? null : MapToDto(ev));
        }

        // ── Helpers ────────────────────────────────────────────────────

        private static BranchEventReadDto MapToDto(BranchEvent e) => new(
            e.Id,
            e.BranchId,
            e.Title,
            e.Description,
            DateOnly.FromDateTime(e.EventDate),
            e.StartTime,
            e.EndTime,
            e.FlyerImageUrl,
            e.ShowPromotionalModal,
            IsAllDay: e.StartTime is null && e.EndTime is null,
            e.IsActive,
            e.CreatedAt
        );

        /// <summary>
        /// Valida que si se proporciona EndTime también haya StartTime,
        /// y que EndTime sea posterior a StartTime.
        /// Devuelve (true, null, null) si no hay error.
        /// </summary>
        private static (bool ok, string? msg, string? key) ValidateTimes(TimeSpan? start, TimeSpan? end)
        {
            if (end.HasValue && !start.HasValue)
                return (false,
                    "Si se especifica hora de fin, también se debe indicar la hora de inicio.",
                    ErrorKeys.EventStartRequiredWithEnd);

            if (start.HasValue && end.HasValue && end.Value <= start.Value)
                return (false,
                    "La hora de fin debe ser posterior a la hora de inicio.",
                    ErrorKeys.EventEndBeforeStart);

            return (true, null, null);
        }
    }
}
