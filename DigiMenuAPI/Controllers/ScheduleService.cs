using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;

        // Nombres en español — índice = DayOfWeek (.NET: 0=Dom … 6=Sáb)
        private static readonly string[] DayNames =
            ["Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];

        public ScheduleService(
            ApplicationDbContext context,
            ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        // ── GET SCHEDULE ──────────────────────────────────────────────
        public async Task<OperationResult<List<BranchScheduleReadDto>>> GetSchedule(
            int branchId)
        {
            await _tenantService.ValidateBranchOwnershipAsync(branchId);

            var schedules = await _context.BranchSchedules
                .AsNoTracking()
                .Where(s => s.BranchId == branchId)
                // Lunes(1) primero, Domingo(0) al final
                .OrderBy(s => s.DayOfWeek == 0 ? 7 : s.DayOfWeek)
                .ToListAsync();

            return OperationResult<List<BranchScheduleReadDto>>.Ok(
                schedules.Select(MapToDto).ToList());
        }

        // ── UPDATE SCHEDULE DAY ───────────────────────────────────────
        public async Task<OperationResult<BranchScheduleReadDto>> UpdateScheduleDay(
            BranchScheduleUpdateDto dto)
        {
            await _tenantService.ValidateBranchOwnershipAsync(dto.BranchId);

            if (dto.DayOfWeek > 6)
                return OperationResult<BranchScheduleReadDto>.ValidationError(
                    "El día debe ser un valor entre 0 (Domingo) y 6 (Sábado).",
                    ErrorKeys.InvalidScheduleDay);

            var schedule = await _context.BranchSchedules
                .FirstOrDefaultAsync(s =>
                    s.BranchId == dto.BranchId &&
                    s.DayOfWeek == dto.DayOfWeek);

            if (schedule is null)
                return OperationResult<BranchScheduleReadDto>.NotFound(
                    "Horario del día no encontrado.",
                    ErrorKeys.ScheduleNotFound);

            if (dto.IsOpen)
            {
                if (dto.OpenTime is null)
                    return OperationResult<BranchScheduleReadDto>.ValidationError(
                        "La hora de apertura es requerida cuando el día está abierto.",
                        ErrorKeys.ScheduleOpenTimeRequired);

                if (dto.CloseTime is null)
                    return OperationResult<BranchScheduleReadDto>.ValidationError(
                        "La hora de cierre es requerida cuando el día está abierto.",
                        ErrorKeys.ScheduleCloseTimeRequired);

                if (dto.CloseTime <= dto.OpenTime)
                    return OperationResult<BranchScheduleReadDto>.ValidationError(
                        "La hora de cierre debe ser posterior a la hora de apertura.",
                        ErrorKeys.ScheduleCloseBeforeOpen);
            }

            schedule.IsOpen = dto.IsOpen;
            schedule.OpenTime = dto.IsOpen ? dto.OpenTime : null;
            schedule.CloseTime = dto.IsOpen ? dto.CloseTime : null;
            await _context.SaveChangesAsync();

            return OperationResult<BranchScheduleReadDto>.Ok(MapToDto(schedule));
        }

        // ── GET SPECIAL DAYS ──────────────────────────────────────────
        public async Task<OperationResult<List<BranchSpecialDayReadDto>>> GetSpecialDays(
            int branchId)
        {
            await _tenantService.ValidateBranchOwnershipAsync(branchId);

            var days = await _context.BranchSpecialDays
                .AsNoTracking()
                .Where(d => d.BranchId == branchId)
                .OrderBy(d => d.Date)
                .Select(d => MapSpecialDayToDto(d))
                .ToListAsync();

            return OperationResult<List<BranchSpecialDayReadDto>>.Ok(days);
        }

        // ── CREATE SPECIAL DAY ────────────────────────────────────────
        public async Task<OperationResult<BranchSpecialDayReadDto>> CreateSpecialDay(
            BranchSpecialDayCreateDto dto)
        {
            await _tenantService.ValidateBranchOwnershipAsync(dto.BranchId);

            if (dto.Date.Date < DateTime.UtcNow.Date)
                return OperationResult<BranchSpecialDayReadDto>.ValidationError(
                    "No se pueden crear días especiales con fecha pasada.",
                    ErrorKeys.SpecialDayPastDate);

            var duplicate = await _context.BranchSpecialDays
                .AnyAsync(d =>
                    d.BranchId == dto.BranchId &&
                    d.Date == dto.Date.Date);

            if (duplicate)
                return OperationResult<BranchSpecialDayReadDto>.Conflict(
                    "Ya existe un día especial registrado para esa fecha en esta sucursal.",
                    ErrorKeys.SpecialDayDuplicate);

            var validationError = ValidateHours(dto.IsClosed, dto.OpenTime, dto.CloseTime);
            if (validationError is not null)
                return OperationResult<BranchSpecialDayReadDto>.ValidationError(
                    validationError.Value.Message, validationError.Value.Key);

            var specialDay = new BranchSpecialDay
            {
                BranchId = dto.BranchId,
                Date = dto.Date.Date,
                IsClosed = dto.IsClosed,
                OpenTime = dto.IsClosed ? null : dto.OpenTime,
                CloseTime = dto.IsClosed ? null : dto.CloseTime,
                Reason = dto.Reason.Trim()
            };

            _context.BranchSpecialDays.Add(specialDay);
            await _context.SaveChangesAsync();

            return OperationResult<BranchSpecialDayReadDto>.Ok(MapSpecialDayToDto(specialDay));
        }

        // ── UPDATE SPECIAL DAY ────────────────────────────────────────
        public async Task<OperationResult<BranchSpecialDayReadDto>> UpdateSpecialDay(
            BranchSpecialDayUpdateDto dto)
        {
            await _tenantService.ValidateBranchOwnershipAsync(dto.BranchId);

            var specialDay = await _context.BranchSpecialDays
                .FirstOrDefaultAsync(d =>
                    d.Id == dto.Id &&
                    d.BranchId == dto.BranchId);

            if (specialDay is null)
                return OperationResult<BranchSpecialDayReadDto>.NotFound(
                    "Día especial no encontrado.",
                    ErrorKeys.SpecialDayNotFound);

            var validationError = ValidateHours(dto.IsClosed, dto.OpenTime, dto.CloseTime);
            if (validationError is not null)
                return OperationResult<BranchSpecialDayReadDto>.ValidationError(
                    validationError.Value.Message, validationError.Value.Key);

            specialDay.IsClosed = dto.IsClosed;
            specialDay.OpenTime = dto.IsClosed ? null : dto.OpenTime;
            specialDay.CloseTime = dto.IsClosed ? null : dto.CloseTime;
            specialDay.Reason = dto.Reason.Trim();
            await _context.SaveChangesAsync();

            return OperationResult<BranchSpecialDayReadDto>.Ok(MapSpecialDayToDto(specialDay));
        }

        // ── DELETE SPECIAL DAY ────────────────────────────────────────
        public async Task<OperationResult<bool>> DeleteSpecialDay(int specialDayId)
        {
            // Cargar primero para poder validar ownership por BranchId
            var specialDay = await _context.BranchSpecialDays
                .FirstOrDefaultAsync(d => d.Id == specialDayId);

            if (specialDay is null)
                return OperationResult<bool>.NotFound(
                    "Día especial no encontrado.",
                    ErrorKeys.SpecialDayNotFound);

            await _tenantService.ValidateBranchOwnershipAsync(specialDay.BranchId);

            // Eliminación física — no hay soft delete en días especiales
            _context.BranchSpecialDays.Remove(specialDay);
            await _context.SaveChangesAsync();

            return OperationResult<bool>.Ok(true);
        }

        // ── Helpers privados ──────────────────────────────────────────

        private static BranchScheduleReadDto MapToDto(BranchSchedule s) =>
            new(s.Id, s.DayOfWeek, DayNames[s.DayOfWeek], s.IsOpen, s.OpenTime, s.CloseTime);

        private static BranchSpecialDayReadDto MapSpecialDayToDto(BranchSpecialDay d) =>
            new(d.Id, d.Date, d.IsClosed, d.OpenTime, d.CloseTime, d.Reason, d.CreatedAt);

        /// <summary>
        /// Valida coherencia de horarios según IsClosed.
        /// Devuelve null si todo es correcto.
        /// </summary>
        private static (string Message, string Key)? ValidateHours(
            bool isClosed, TimeSpan? openTime, TimeSpan? closeTime)
        {
            if (isClosed) return null; // cerrado → no se validan horarios

            if (openTime is null)
                return ("La hora de apertura es requerida para un día con horario especial.",
                        ErrorKeys.SpecialDayHoursRequired);

            if (closeTime is null)
                return ("La hora de cierre es requerida para un día con horario especial.",
                        ErrorKeys.SpecialDayHoursRequired);

            if (closeTime <= openTime)
                return ("La hora de cierre debe ser posterior a la hora de apertura.",
                        ErrorKeys.SpecialDayCloseBeforeOpen);

            return null;
        }
    }
}