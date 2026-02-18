using AutoMapper;
using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.AddDTOs;
using DigiMenuAPI.Application.DTOs.ReadDTOs;
using DigiMenuAPI.Application.DTOs.UpdateDTOs;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services
{
    public class FooterLinkService : IFooterLinkService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IOutputCacheStore _cacheStore;
        private const string CacheTag = "tag-menu-publico";

        public FooterLinkService(ApplicationDbContext context, IMapper mapper, IOutputCacheStore cacheStore)
        {
            _context = context;
            _mapper = mapper;
            _cacheStore = cacheStore;
        }

        public async Task<OperationResult<List<FooterLinkReadDto>>> GetAll()
        {
            var links = await _context.FooterLinks
                .AsNoTracking()
                .Include(f => f.StandardIcon)
                .OrderBy(f => f.DisplayOrder)
                .Select(f => new FooterLinkReadDto(
                    f.Id,
                    f.Label,
                    f.Url,
                    f.StandardIcon != null ? f.StandardIcon.SvgContent : (f.CustomSvgContent ?? ""),
                    f.DisplayOrder
                ))
                .ToListAsync();

            return OperationResult<List<FooterLinkReadDto>>.Ok(links);
        }
        private async Task<OperationResult<FooterLinkReadDto>> GetById(int id)
        {
            var link = await _context.FooterLinks
                .Include(f => f.StandardIcon)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (link is null)
                return OperationResult<FooterLinkReadDto>.Fail("No encontrado");

            return OperationResult<FooterLinkReadDto>.Ok(new FooterLinkReadDto(
                link.Id,
                link.Label,
                link.Url,
                link.StandardIcon is not null ? link.StandardIcon.SvgContent : (link.CustomSvgContent ?? ""),
                link.DisplayOrder)
            );
        }

        public async Task<OperationResult<FooterLinkReadDto>> Create(FooterLinkCreateDto dto)
        {
            var link = _mapper.Map<FooterLink>(dto);
            _context.FooterLinks.Add(link);
            await _context.SaveChangesAsync();

            await _cacheStore.EvictByTagAsync(CacheTag, default);

            return await GetById(link.Id);
        }

        public async Task<OperationResult<bool>> Update(FooterLinkUpdateDto dto)
        {
            var link = await _context.FooterLinks.FindAsync(dto.Id);
            if (link is null)
                return OperationResult<bool>.Fail("Enlace no encontrado");

            _mapper.Map(dto, link);
            await _context.SaveChangesAsync();

            await _cacheStore.EvictByTagAsync(CacheTag, default);
            return OperationResult<bool>.Ok(true);
        }

        public async Task<OperationResult<bool>> Delete(int id)
        {
            var link = await _context.FooterLinks.FindAsync(id);
            if (link is null)
                return OperationResult<bool>.Fail("Enlace no encontrado");

            link.IsDeleted = true;
            await _context.SaveChangesAsync();

            await _cacheStore.EvictByTagAsync(CacheTag, default);
            return OperationResult<bool>.Ok(true);
        }
    }
}