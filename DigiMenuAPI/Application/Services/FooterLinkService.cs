using AutoMapper;
using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using AppCore.Application.Interfaces;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services
{
    public class FooterLinkService : IFooterLinkService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ITenantService _tenantService;
        private readonly ICacheService _cache;

        public FooterLinkService(
            ApplicationDbContext context,
            IMapper mapper,
            ITenantService tenantService,
            ICacheService cache)
        {
            _context = context;
            _mapper = mapper;
            _tenantService = tenantService;
            _cache = cache;
        }

        public async Task<OperationResult<List<FooterLinkReadDto>>> GetAll(int branchId)
        {
            // Valida que la Branch pertenece al tenant autenticado
            await _tenantService.ValidateBranchOwnershipAsync(branchId);

            // QueryFilter global ya aplica !IsDeleted — solo filtramos por Branch
            var links = await _context.FooterLinks
                .AsNoTracking()
                .Where(f => f.BranchId == branchId)
                .Include(f => f.StandardIcon)
                .OrderBy(f => f.DisplayOrder)
                .Select(f => new FooterLinkReadDto(
                    f.Id,
                    f.Label,
                    f.Url,
                    f.StandardIcon != null ? f.StandardIcon.SvgContent : (f.CustomSvgContent ?? ""),
                    f.DisplayOrder))
                .ToListAsync();

            return OperationResult<List<FooterLinkReadDto>>.Ok(links);
        }

        public async Task<OperationResult<FooterLinkReadDto>> Create(FooterLinkCreateDto dto)
        {
            // Valida ownership antes de crear
            await _tenantService.ValidateBranchOwnershipAsync(dto.BranchId);

            var link = _mapper.Map<FooterLink>(dto);
            _context.FooterLinks.Add(link);
            await _context.SaveChangesAsync();

            await _cache.EvictMenuByBranchAsync(dto.BranchId);

            return await GetByIdInternal(link.Id);
        }

        public async Task<OperationResult<bool>> Update(FooterLinkUpdateDto dto)
        {
            var companyId = _tenantService.GetCompanyId();

            // Valida ownership via join con Branch — QueryFilter cubre !IsDeleted
            var link = await _context.FooterLinks
                .Include(f => f.Branch)
                .FirstOrDefaultAsync(f => f.Id == dto.Id && f.Branch.CompanyId == companyId);

            if (link is null)
                return OperationResult<bool>.NotFound("Enlace no encontrado.", errorKey: ErrorKeys.FooterLinkNotFound);

            // BranchAdmin/Staff: validar que el link pertenece a su propia Branch
            var ownBranchId = _tenantService.TryGetBranchId();
            if (ownBranchId.HasValue && link.BranchId != ownBranchId.Value)
                return OperationResult<bool>.Forbidden("No tienes permiso para modificar este enlace.", errorKey: ErrorKeys.FooterLinkNotOwned);

            _mapper.Map(dto, link);
            await _context.SaveChangesAsync();

            await _cache.EvictMenuByBranchAsync(link.BranchId);

            return OperationResult<bool>.Ok(true);
        }

        public async Task<OperationResult<bool>> Delete(int id)
        {
            var companyId = _tenantService.GetCompanyId();

            var link = await _context.FooterLinks
                .Include(f => f.Branch)
                .FirstOrDefaultAsync(f => f.Id == id && f.Branch.CompanyId == companyId);

            if (link is null)
                return OperationResult<bool>.NotFound("Enlace no encontrado.", errorKey: ErrorKeys.FooterLinkNotFound);

            // BranchAdmin/Staff: validar que el link pertenece a su propia Branch
            var ownBranchId = _tenantService.TryGetBranchId();
            if (ownBranchId.HasValue && link.BranchId != ownBranchId.Value)
                return OperationResult<bool>.Forbidden("No tienes permiso para eliminar este enlace.", errorKey: ErrorKeys.TagNotOwned);

            link.IsDeleted = true;
            await _context.SaveChangesAsync();

            await _cache.EvictMenuByBranchAsync(link.BranchId);

            return OperationResult<bool>.Ok(true);
        }

        // ── Helper privado ────────────────────────────────────────────

        private async Task<OperationResult<FooterLinkReadDto>> GetByIdInternal(int id)
        {
            // QueryFilter ya cubre !IsDeleted
            var link = await _context.FooterLinks
                .Include(f => f.StandardIcon)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (link is null)
                return OperationResult<FooterLinkReadDto>.Fail("Enlace no encontrado.");

            return OperationResult<FooterLinkReadDto>.Ok(new FooterLinkReadDto(
                link.Id,
                link.Label,
                link.Url,
                link.StandardIcon is not null
                    ? link.StandardIcon.SvgContent
                    : (link.CustomSvgContent ?? ""),
                link.DisplayOrder));
        }
    }
}