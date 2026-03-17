using AppCore.Application.Common;
using AppCore.Application.Interfaces;
using AppCore.Domain.Entities;
using AppCore.Infrastructure.SQL;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services
{
    public class CompanyLanguageService : ICompanyLanguageService
    {
        private readonly CoreDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly ICacheService _cache;

        public CompanyLanguageService(
            CoreDbContext context,
            ITenantService tenantService,
            ICacheService cache)
        {
            _context = context;
            _tenantService = tenantService;
            _cache = cache;
        }

        public async Task<OperationResult<List<SupportedLanguageReadDto>>> GetSupportedLanguages()
        {
            var companyId = _tenantService.GetCompanyId();

            var supported = await _context.SupportedLanguages
                .AsNoTracking()
                .Where(l => l.IsActive)
                .OrderBy(l => l.DisplayOrder)
                .ToListAsync();

            var companyLangs = await _context.CompanyLanguages
                .AsNoTracking()
                .Where(cl => cl.CompanyId == companyId)
                .ToListAsync();

            var result = supported.Select(l =>
            {
                var companyLang = companyLangs.FirstOrDefault(cl => cl.LanguageCode == l.Code);
                return new SupportedLanguageReadDto(
                    l.Code,
                    l.Name,
                    l.Flag,
                    l.DisplayOrder,
                    l.IsActive,
                    IsSelected: companyLang != null,
                    IsDefault: companyLang?.IsDefault ?? false);
            }).ToList();

            return OperationResult<List<SupportedLanguageReadDto>>.Ok(result);
        }

        public async Task<OperationResult<List<CompanyLanguageReadDto>>> GetCompanyLanguages()
        {
            var companyId = _tenantService.GetCompanyId();
            return OperationResult<List<CompanyLanguageReadDto>>.Ok(
                await GetCompanyLanguagesInternal(companyId));
        }

        public async Task<OperationResult<List<CompanyLanguageReadDto>>> AddLanguage(string code)
        {
            var companyId = _tenantService.GetCompanyId();
            code = code.Trim().ToLowerInvariant();

            var supported = await _context.SupportedLanguages
                .FirstOrDefaultAsync(l => l.Code == code && l.IsActive);

            if (supported is null)
                return OperationResult<List<CompanyLanguageReadDto>>.Fail(
                    $"El idioma '{code}' no está soportado por la plataforma.");

            var alreadyExists = await _context.CompanyLanguages
                .AnyAsync(cl => cl.CompanyId == companyId && cl.LanguageCode == code);

            if (alreadyExists)
                return OperationResult<List<CompanyLanguageReadDto>>.Fail(
                    $"El idioma '{code}' ya está habilitado para esta empresa.");

            // Si es el primer idioma, queda automáticamente como default
            var hasAny = await _context.CompanyLanguages
                .AnyAsync(cl => cl.CompanyId == companyId);

            _context.CompanyLanguages.Add(new CompanyLanguage
            {
                CompanyId = companyId,
                LanguageCode = code,
                IsDefault = !hasAny
            });

            await _context.SaveChangesAsync();
            await _cache.EvictMenuByCompanyAsync(companyId);

            return OperationResult<List<CompanyLanguageReadDto>>.Ok(
                await GetCompanyLanguagesInternal(companyId));
        }

        public async Task<OperationResult<List<CompanyLanguageReadDto>>> RemoveLanguage(string code)
        {
            var companyId = _tenantService.GetCompanyId();
            code = code.Trim().ToLowerInvariant();

            var lang = await _context.CompanyLanguages
                .FirstOrDefaultAsync(cl =>
                    cl.CompanyId == companyId &&
                    cl.LanguageCode == code);

            if (lang is null)
                return OperationResult<List<CompanyLanguageReadDto>>.Fail(
                    $"El idioma '{code}' no está habilitado para esta empresa.");

            var totalActive = await _context.CompanyLanguages
                .CountAsync(cl => cl.CompanyId == companyId);

            if (totalActive <= 1)
                return OperationResult<List<CompanyLanguageReadDto>>.Fail(
                    "La empresa debe tener al menos un idioma habilitado.");

            if (lang.IsDefault)
                return OperationResult<List<CompanyLanguageReadDto>>.Fail(
                    "No puedes eliminar el idioma por defecto. Establece otro idioma como defecto primero.");

            _context.CompanyLanguages.Remove(lang);

            await _context.SaveChangesAsync();
            await _cache.EvictMenuByCompanyAsync(companyId);

            return OperationResult<List<CompanyLanguageReadDto>>.Ok(
                await GetCompanyLanguagesInternal(companyId));
        }

        public async Task<OperationResult<List<CompanyLanguageReadDto>>> SetDefault(string code)
        {
            var companyId = _tenantService.GetCompanyId();
            code = code.Trim().ToLowerInvariant();

            var newDefault = await _context.CompanyLanguages
                .FirstOrDefaultAsync(cl =>
                    cl.CompanyId == companyId &&
                    cl.LanguageCode == code);

            if (newDefault is null)
                return OperationResult<List<CompanyLanguageReadDto>>.Fail(
                    $"El idioma '{code}' no está habilitado para esta empresa.");

            if (newDefault.IsDefault)
                return OperationResult<List<CompanyLanguageReadDto>>.Ok(
                    await GetCompanyLanguagesInternal(companyId));

            // Quitar default al idioma anterior
            var currentDefault = await _context.CompanyLanguages
                .FirstOrDefaultAsync(cl =>
                    cl.CompanyId == companyId &&
                    cl.IsDefault);

            if (currentDefault is not null)
                currentDefault.IsDefault = false;

            newDefault.IsDefault = true;

            await _context.SaveChangesAsync();
            await _cache.EvictMenuByCompanyAsync(companyId);

            return OperationResult<List<CompanyLanguageReadDto>>.Ok(
                await GetCompanyLanguagesInternal(companyId));
        }

        // ── Helpers ───────────────────────────────────────────────────

        private async Task<List<CompanyLanguageReadDto>> GetCompanyLanguagesInternal(int companyId)
            => await _context.CompanyLanguages
                .AsNoTracking()
                .Where(cl => cl.CompanyId == companyId)
                .Include(cl => cl.Language)
                .OrderBy(cl => cl.Language.DisplayOrder)
                .Select(cl => new CompanyLanguageReadDto(
                    cl.LanguageCode,
                    cl.Language.Name,
                    cl.Language.Flag,
                    cl.IsDefault))
                .ToListAsync();
    }
}
