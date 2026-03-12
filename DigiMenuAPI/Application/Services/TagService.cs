using AutoMapper;
using AutoMapper.QueryableExtensions;
using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using AppCore.Application.Interfaces;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services
{
    public class TagService : ITagService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ITenantService _tenantService;
        private readonly IOutputCacheStore _cacheStore;
        private const string CacheTag = "tag-menu-publico";

        public TagService(
            ApplicationDbContext context,
            IMapper mapper,
            ITenantService tenantService,
            IOutputCacheStore cacheStore)
        {
            _context = context;
            _mapper = mapper;
            _tenantService = tenantService;
            _cacheStore = cacheStore;
        }

        public async Task<OperationResult<List<TagReadDto>>> GetAll()
        {
            var companyId = _tenantService.GetCompanyId();

            // QueryFilter global ya aplica !IsDeleted — solo falta filtrar por tenant
            var tags = await _context.Tags
                .AsNoTracking()
                .Where(t => t.CompanyId == companyId)
                .ProjectTo<TagReadDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return OperationResult<List<TagReadDto>>.Ok(tags);
        }

        public async Task<OperationResult<TagReadDto>> GetById(int id)
        {
            var companyId = _tenantService.GetCompanyId();

            var tag = await _context.Tags
                .AsNoTracking()
                .Where(t => t.Id == id && t.CompanyId == companyId)
                .ProjectTo<TagReadDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            if (tag is null)
                return OperationResult<TagReadDto>.Fail("Etiqueta no encontrada.");

            return OperationResult<TagReadDto>.Ok(tag);
        }

        public async Task<OperationResult<TagReadDto>> Create(TagCreateDto dto)
        {
            var companyId = _tenantService.GetCompanyId();

            var tag = _mapper.Map<Tag>(dto);
            tag.CompanyId = companyId; // ← siempre desde JWT, nunca del cliente

            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();

            await _cacheStore.EvictByTagAsync(CacheTag, default);

            return OperationResult<TagReadDto>.Ok(_mapper.Map<TagReadDto>(tag));
        }

        public async Task<OperationResult<bool>> Update(TagUpdateDto dto)
        {
            var companyId = _tenantService.GetCompanyId();

            var tag = await _context.Tags
                .FirstOrDefaultAsync(t => t.Id == dto.Id && t.CompanyId == companyId);

            if (tag is null)
                return OperationResult<bool>.NotFound("Etiqueta no encontrada.", errorKey: ErrorKeys.TagNotFound);

            _mapper.Map(dto, tag);
            await _context.SaveChangesAsync();

            await _cacheStore.EvictByTagAsync(CacheTag, default);

            return OperationResult<bool>.Ok(true);
        }

        public async Task<OperationResult<bool>> Delete(int id)
        {
            var companyId = _tenantService.GetCompanyId();

            var tag = await _context.Tags
                .FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId);

            if (tag is null)
                return OperationResult<bool>.NotFound("Etiqueta no encontrada.", errorKey: ErrorKeys.TagNotFound);

            tag.IsDeleted = true;
            await _context.SaveChangesAsync();

            await _cacheStore.EvictByTagAsync(CacheTag, default);

            return OperationResult<bool>.Ok(true);
        }
    }
}