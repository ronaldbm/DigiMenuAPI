using AutoMapper;
using AutoMapper.QueryableExtensions;
using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.AddDTOs;
using DigiMenuAPI.Application.DTOs.ReadDTOs;
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

        public ReservationService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<OperationResult<List<ReservationReadDto>>> GetAll()
        {
            var list = await _context.Reservations
                .AsNoTracking()
                .OrderByDescending(r => r.ReservationDate)
                .ProjectTo<ReservationReadDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
            return OperationResult<List<ReservationReadDto>>.Ok(list);
        }

        public async Task<OperationResult<int>> Create(ReservationCreateDto dto)
        {
            // Validación básica: No permitir fechas pasadas
            if (dto.ReservationDate.Date < DateTime.UtcNow.Date)
                return OperationResult<int>.Fail("No se pueden realizar reservas para fechas pasadas.");

            var reservation = _mapper.Map<Reservation>(dto);
            reservation.Status = 1; // Siempre inicia como Pendiente

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            return OperationResult<int>.Ok(reservation.Id);
        }

        public async Task<OperationResult<bool>> UpdateStatus(int id, byte newStatus)
        {
            var res = await _context.Reservations.FindAsync(id);
            if (res is null) 
                return OperationResult<bool>.Fail("Reserva no encontrada");

            res.Status = newStatus;
            await _context.SaveChangesAsync();
            return OperationResult<bool>.Ok(true);
        }

        public async Task<OperationResult<bool>> Delete(int id)
        {
            var res = await _context.Reservations.FindAsync(id);
            if (res is null) 
                return OperationResult<bool>.Fail("Reserva no encontrada");

            res.IsDeleted = true;
            await _context.SaveChangesAsync();
            return OperationResult<bool>.Ok(true);
        }
    }
}
