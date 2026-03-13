using AutoMapper;
using AutoMapper.QueryableExtensions;
using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.Interfaces;
using AppCore.Application.Interfaces;
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

        public async Task<OperationResult<PagedResult<ReservationReadDto>>> GetAll(int page = 1, int pageSize = 20)
        {
            var companyId = _tenantService.GetCompanyId();
            var branchId = _tenantService.TryGetBranchId(); // null = CompanyAdmin (ve todas)

            // Reservation no tiene CompanyId propio — se filtra via Branch.CompanyId
            // QueryFilter global ya aplica !IsDeleted
            var query = _context.Reservations
                .AsNoTracking()
                .Where(r => r.Branch.CompanyId == companyId);

            // BranchAdmin (2) y Staff (3) solo ven su propia Branch
            if (branchId.HasValue)
                query = query.Where(r => r.BranchId == branchId.Value);

            var orderedQuery = query
                .OrderByDescending(r => r.ReservationDate)
                .ThenByDescending(r => r.ReservationTime);

            var total = await query.CountAsync();
            var data = await orderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ProjectTo<ReservationReadDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return OperationResult<PagedResult<ReservationReadDto>>.Ok(
                PagedResult<ReservationReadDto>.Create(data, total, page, pageSize));
        }

        public async Task<OperationResult<int>> Create(ReservationCreateDto dto, int branchId, int companyId)
        {
            // Verificar que la empresa tiene el módulo de reservas activo
            await _moduleGuard.AssertModuleAsync(companyId, ModuleCodes.Reservations);

            if (dto.ReservationDate.Date < DateTime.UtcNow.Date)
                return OperationResult<int>.Fail("No se pueden realizar reservas para fechas pasadas.");

            // Validar que la Branch pertenece a la empresa resuelta (endpoint público)
            var branchBelongs = await _context.Branches
                .AnyAsync(b => b.Id == branchId && b.CompanyId == companyId && b.IsActive);

            if (!branchBelongs)
                return OperationResult<int>.Fail("La sucursal no existe o no está disponible.");

            var reservation = _mapper.Map<Reservation>(dto);
            reservation.BranchId = branchId; // garantizado desde la resolución pública
            reservation.Status = ReservationStatus.Pending;

            // NOTA: Reservation NO tiene CompanyId — la empresa se resuelve via Branch.CompanyId

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            return OperationResult<int>.Ok(reservation.Id);
        }

        public async Task<OperationResult<bool>> UpdateStatus(int id, ReservationStatus newStatus)
        {
            var companyId = _tenantService.GetCompanyId();
            var branchId = _tenantService.TryGetBranchId();

            // Validar ownership via Branch.CompanyId — QueryFilter cubre !IsDeleted
            var res = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == id && r.Branch.CompanyId == companyId);

            if (res is null)
                return OperationResult<bool>.NotFound("Reserva no encontrada.", errorKey: ErrorKeys.ReservationNotFound);

            // BranchAdmin/Staff solo pueden operar sobre su Branch
            if (branchId.HasValue && res.BranchId != branchId.Value)
                return OperationResult<bool>.Forbidden("No tienes permiso para modificar esta reserva.", errorKey: ErrorKeys.Forbidden);

            res.Status = newStatus;
            await _context.SaveChangesAsync();

            return OperationResult<bool>.Ok(true);
        }

        public async Task<OperationResult<bool>> Delete(int id)
        {
            var companyId = _tenantService.GetCompanyId();
            var branchId = _tenantService.TryGetBranchId();

            var res = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == id && r.Branch.CompanyId == companyId);

            if (res is null)
                return OperationResult<bool>.NotFound("Reserva no encontrada.", errorKey: ErrorKeys.ReservationNotFound);

            if (branchId.HasValue && res.BranchId != branchId.Value)
                return OperationResult<bool>.Forbidden("No tienes permiso para eliminar esta reserva.", errorKey: ErrorKeys.Forbidden);

            res.IsDeleted = true;
            await _context.SaveChangesAsync();

            return OperationResult<bool>.Ok(true);
        }
    }
}