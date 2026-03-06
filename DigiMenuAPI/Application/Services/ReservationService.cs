using AutoMapper;
using AutoMapper.QueryableExtensions;
using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services
{
    public class ReservationService : IReservationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ITenantService _tenantService;
        private readonly IModuleGuard _moduleGuard;

        public ReservationService(
            ApplicationDbContext context,
            IMapper mapper,
            ITenantService tenantService,
            IModuleGuard moduleGuard)
        {
            _context = context;
            _mapper = mapper;
            _tenantService = tenantService;
            _moduleGuard = moduleGuard;
        }

        public async Task<OperationResult<List<ReservationReadDto>>> GetAll()
        {
            // El Query Filter global ya filtra por CompanyId
            var list = await _context.Reservations
                .AsNoTracking()
                .OrderByDescending(r => r.ReservationDate)
                .ThenByDescending(r => r.ReservationTime)
                .ProjectTo<ReservationReadDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return OperationResult<List<ReservationReadDto>>.Ok(list);
        }

        public async Task<OperationResult<int>> Create(ReservationCreateDto dto, int companyId)
        {
            // Verificar módulo activo (endpoint público puede llamar con companyId explícito)
            await _moduleGuard.AssertModuleAsync(companyId, ModuleCodes.Reservations);

            if (dto.ReservationDate.Date < DateTime.UtcNow.Date)
                return OperationResult<int>.Fail("No se pueden realizar reservas para fechas pasadas.");

            var reservation = _mapper.Map<Reservation>(dto);
            reservation.Status = 1;
            reservation.CompanyId = companyId;

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            return OperationResult<int>.Ok(reservation.Id);
        }

        public async Task<OperationResult<bool>> UpdateStatus(int id, byte newStatus)
        {
            var res = await _context.Reservations.FindAsync(id);
            if (res is null)
                return OperationResult<bool>.Fail("Reserva no encontrada.");

            res.Status = newStatus;
            await _context.SaveChangesAsync();

            return OperationResult<bool>.Ok(true);
        }

        public async Task<OperationResult<bool>> Delete(int id)
        {
            var res = await _context.Reservations.FindAsync(id);
            if (res is null)
                return OperationResult<bool>.Fail("Reserva no encontrada.");

            res.IsDeleted = true;
            await _context.SaveChangesAsync();

            return OperationResult<bool>.Ok(true);
        }
    }
}