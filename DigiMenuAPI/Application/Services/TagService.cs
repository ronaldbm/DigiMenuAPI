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

        public async Task<OperationResult<List<TagListItemDto>>> GetAll(string? lang = null)
        {
            var companyId = _tenantService.GetCompanyId();

            var tags = await _context.Tags
                .AsNoTracking()
                .Include(t => t.Translations)
                .Where(t => t.CompanyId == companyId)
                .ToListAsync();

            var result = tags.Select(t => new TagListItemDto
            {
                Id        = t.Id,
                CompanyId = t.CompanyId,
                Color     = t.Color,
                Name      = ResolveName(t.Translations, lang),
            }).ToList();

            return OperationResult<List<TagListItemDto>>.Ok(result);
        }

        public async Task<OperationResult<TagReadDto>> GetById(int id)
        {
            var companyId = _tenantService.GetCompanyId();

            var tag = await _context.Tags
                .AsNoTracking()
                .Include(t => t.Translations)
                .Where(t => t.Id == id && t.CompanyId == companyId)
                .ProjectTo<TagReadDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            if (tag is null)
                return OperationResult<TagReadDto>.NotFound("Etiqueta no encontrada.", errorKey: ErrorKeys.TagNotFound);

            return OperationResult<TagReadDto>.Ok(tag);
        }

        public async Task<OperationResult<TagReadDto>> Create(TagCreateDto dto)
        {
            var companyId = _tenantService.GetCompanyId();

            await using var tx = await _context.Database.BeginTransactionAsync();

            var tag = _mapper.Map<Tag>(dto);
            tag.CompanyId = companyId;

            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();

            foreach (var t in dto.Translations.Where(t => !string.IsNullOrWhiteSpace(t.Name)))
            {
                _context.TagTranslations.Add(new TagTranslation
                {
                    TagId        = tag.Id,
                    LanguageCode = t.LanguageCode.Trim().ToLowerInvariant(),
                    Name         = t.Name.Trim(),
                });
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            await _cacheStore.EvictByTagAsync(CacheTag, default);

            await _context.Entry(tag).Collection(t => t.Translations).LoadAsync();
            return OperationResult<TagReadDto>.Ok(_mapper.Map<TagReadDto>(tag));
        }

        public async Task<OperationResult<bool>> Update(TagUpdateDto dto)
        {
            var companyId = _tenantService.GetCompanyId();

            var tag = await _context.Tags
                .Include(t => t.Translations)
                .FirstOrDefaultAsync(t => t.Id == dto.Id && t.CompanyId == companyId);

            if (tag is null)
                return OperationResult<bool>.NotFound("Etiqueta no encontrada.", errorKey: ErrorKeys.TagNotFound);

            await using var tx = await _context.Database.BeginTransactionAsync();

            tag.Color = dto.Color ?? "#ffffff";

            var incoming = dto.Translations
                .Where(t => !string.IsNullOrWhiteSpace(t.Name))
                .ToDictionary(t => t.LanguageCode.Trim().ToLowerInvariant());

            var toDelete = tag.Translations
                .Where(t => !incoming.ContainsKey(t.LanguageCode))
                .ToList();
            _context.TagTranslations.RemoveRange(toDelete);

            foreach (var (code, input) in incoming)
            {
                var existing = tag.Translations.FirstOrDefault(t => t.LanguageCode == code);
                if (existing is null)
                {
                    _context.TagTranslations.Add(new TagTranslation
                    {
                        TagId        = tag.Id,
                        LanguageCode = code,
                        Name         = input.Name.Trim(),
                    });
                }
                else
                {
                    existing.Name = input.Name.Trim();
                }
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

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

        // ── Helpers ───────────────────────────────────────────────────

        private static string ResolveName(IEnumerable<TagTranslation> translations, string? lang)
        {
            var list = translations.ToList();
            if (!string.IsNullOrWhiteSpace(lang))
            {
                var match = list.FirstOrDefault(t => t.LanguageCode == lang);
                if (match is not null) return match.Name;
            }
            return list.FirstOrDefault()?.Name ?? string.Empty;
        }
    }
}
