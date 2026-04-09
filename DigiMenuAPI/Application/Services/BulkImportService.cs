using System.IO.Compression;
using AppCore.Application.Common;
using AppCore.Application.Interfaces;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Services
{
    public class BulkImportService : IBulkImportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly IFileStorageService _fileStorage;
        private readonly ICacheService _cache;
        private readonly ImportLockService _importLock;

        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
            { ".jpg", ".jpeg", ".png", ".webp" };

        private static readonly HashSet<string> DangerousExtensions = new(StringComparer.OrdinalIgnoreCase)
            { ".exe", ".bat", ".sh", ".cmd", ".ps1", ".dll", ".js", ".php", ".asp", ".jsp", ".msi", ".com" };

        private const long MaxZipSize = 50 * 1024 * 1024;          // 50 MB
        private const long MaxImageSize = 5 * 1024 * 1024;          // 5 MB
        private const long MaxUncompressedSize = 200 * 1024 * 1024;  // 200 MB
        private const int MaxRowsPerImport = 500;

        public BulkImportService(
            ApplicationDbContext context,
            ITenantService tenantService,
            IFileStorageService fileStorage,
            ICacheService cache,
            ImportLockService importLock)
        {
            _context = context;
            _tenantService = tenantService;
            _fileStorage = fileStorage;
            _cache = cache;
            _importLock = importLock;
        }

        // ══════════════════════════════════════════════════════════════
        //  TEMPLATES
        // ══════════════════════════════════════════════════════════════

        public async Task<OperationResult<CsvTemplateDto>> GetCategoryTemplate()
        {
            var (langs, err) = await GetCompanyLanguages();
            if (err is not null) return OperationResult<CsvTemplateDto>.ValidationError(err, ErrorKeys.BulkImportNoLanguages);

            var headers = new List<string>();
            foreach (var lang in langs!) headers.Add($"nombre_{lang}");
            headers.Add("visible");

            return OperationResult<CsvTemplateDto>.Ok(new CsvTemplateDto { Headers = headers, ImportType = "categories" });
        }

        public async Task<OperationResult<CsvTemplateDto>> GetProductTemplate()
        {
            var (langs, err) = await GetCompanyLanguages();
            if (err is not null) return OperationResult<CsvTemplateDto>.ValidationError(err, ErrorKeys.BulkImportNoLanguages);

            var headers = new List<string> { "categoria" };
            foreach (var lang in langs!) headers.Add($"nombre_{lang}");
            foreach (var lang in langs!) headers.Add($"desc_corta_{lang}");
            foreach (var lang in langs!) headers.Add($"desc_larga_{lang}");
            headers.Add("imagen");

            return OperationResult<CsvTemplateDto>.Ok(new CsvTemplateDto { Headers = headers, ImportType = "products" });
        }

        public async Task<OperationResult<CsvTemplateDto>> GetBranchProductTemplate()
        {
            return await Task.FromResult(OperationResult<CsvTemplateDto>.Ok(new CsvTemplateDto
            {
                Headers = ["producto", "sucursal", "categoria", "precio", "precio_oferta", "visible", "imagen"],
                ImportType = "branch-products"
            }));
        }

        // ══════════════════════════════════════════════════════════════
        //  IMPORT CATEGORIES
        // ══════════════════════════════════════════════════════════════

        public async Task<OperationResult<BulkImportResultDto>> ImportCategories(BulkCategoryImportDto dto)
        {
            var companyId = _tenantService.GetCompanyId();
            var semaphore = _importLock.GetLock(companyId);

            if (!await semaphore.WaitAsync(TimeSpan.Zero))
                return OperationResult<BulkImportResultDto>.Conflict(
                    "Ya hay una importación en progreso para esta empresa. Espera a que termine.",
                    ErrorKeys.BulkImportAlreadyInProgress);

            try
            {
                return await ImportCategoriesInternal(dto, companyId);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task<OperationResult<BulkImportResultDto>> ImportCategoriesInternal(
            BulkCategoryImportDto dto, int companyId)
        {
            if (dto.Items.Count > MaxRowsPerImport)
                return OperationResult<BulkImportResultDto>.ValidationError(
                    $"El máximo es {MaxRowsPerImport} filas por importación.",
                    ErrorKeys.BulkImportValidationFailed);

            // V3.2 — Company languages
            var companyLangs = await _context.CompanyLanguages
                .Where(cl => cl.CompanyId == companyId)
                .Select(cl => new { cl.LanguageCode, cl.IsDefault })
                .ToListAsync();

            var defaultLang = companyLangs.FirstOrDefault(l => l.IsDefault)?.LanguageCode;
            if (defaultLang is null)
                return OperationResult<BulkImportResultDto>.ValidationError(
                    "La empresa no tiene un idioma predeterminado configurado.",
                    ErrorKeys.BulkImportNoLanguages);

            var validLangs = companyLangs.Select(l => l.LanguageCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var errors = new List<BulkImportRowError>();
            var warnings = new List<BulkImportRowWarning>();

            // V3.4 — Existing category names
            var existingNames = await _context.CategoryTranslations
                .Where(ct => ct.Category.CompanyId == companyId && ct.LanguageCode == defaultLang)
                .Select(ct => ct.Name.Trim().ToLower())
                .ToListAsync();
            var existingSet = existingNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

            // V3.5 — Duplicates within batch
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < dto.Items.Count; i++)
            {
                var item = dto.Items[i];
                var row = i + 1;

                // V3.2 — Default lang required
                if (!item.Names.TryGetValue(defaultLang, out var defName) || string.IsNullOrWhiteSpace(defName))
                {
                    errors.Add(new BulkImportRowError
                    {
                        Row = row, Field = $"nombre_{defaultLang}",
                        ErrorKey = ErrorKeys.BulkImportDefaultLangRequired,
                        Message = $"El nombre en el idioma predeterminado ({defaultLang}) es obligatorio."
                    });
                    continue;
                }

                var trimmedName = defName.Trim();

                // V3.9 — Length validation
                foreach (var (lang, name) in item.Names)
                {
                    if (!string.IsNullOrWhiteSpace(name) && name.Trim().Length > 100)
                        errors.Add(new BulkImportRowError
                        {
                            Row = row, Field = $"nombre_{lang}",
                            ErrorKey = ErrorKeys.ValidationFailed,
                            Message = $"El nombre excede el máximo de 100 caracteres."
                        });
                }

                // V3.5 — Duplicate in batch
                if (!seen.Add(trimmedName))
                {
                    errors.Add(new BulkImportRowError
                    {
                        Row = row, Field = $"nombre_{defaultLang}",
                        ErrorKey = ErrorKeys.BulkImportDuplicateRow,
                        Message = $"Nombre duplicado dentro del archivo: '{trimmedName}'."
                    });
                    continue;
                }

                // V3.4 — Duplicate against DB
                if (existingSet.Contains(trimmedName))
                {
                    warnings.Add(new BulkImportRowWarning
                    {
                        Row = row, Field = $"nombre_{defaultLang}",
                        WarningKey = ErrorKeys.BulkImportDuplicateInDb,
                        Message = $"Ya existe una categoría con el nombre '{trimmedName}'."
                    });
                }
            }

            if (errors.Count > 0)
                return OperationResult<BulkImportResultDto>.ValidationError(
                    "Se encontraron errores de validación.",
                    ErrorKeys.BulkImportValidationFailed,
                    new BulkImportResultDto { Errors = errors, Warnings = warnings });

            // ── Insert transactional ──
            var created = 0;
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();

                var maxOrder = await _context.Categories
                    .Where(c => c.CompanyId == companyId)
                    .MaxAsync(c => (int?)c.DisplayOrder) ?? 0;

                var categories = new List<Category>();
                foreach (var (item, idx) in dto.Items.Select((v, i) => (v, i)))
                {
                    categories.Add(new Category
                    {
                        CompanyId = companyId,
                        IsVisible = item.IsVisible,
                        DisplayOrder = maxOrder + idx + 1,
                    });
                }

                _context.Categories.AddRange(categories);
                await _context.SaveChangesAsync();

                var translations = new List<CategoryTranslation>();
                foreach (var (cat, item) in categories.Zip(dto.Items))
                {
                    foreach (var (lang, name) in item.Names)
                    {
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        if (!validLangs.Contains(lang)) continue;

                        translations.Add(new CategoryTranslation
                        {
                            CategoryId = cat.Id,
                            LanguageCode = lang.Trim().ToLowerInvariant(),
                            Name = name.Trim(),
                        });
                    }
                    created++;
                }

                _context.CategoryTranslations.AddRange(translations);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            });

            await _cache.EvictMenuByCompanyAsync(companyId);

            return OperationResult<BulkImportResultDto>.Ok(new BulkImportResultDto
            {
                CreatedCount = created,
                Warnings = warnings
            });
        }

        // ══════════════════════════════════════════════════════════════
        //  IMPORT PRODUCTS
        // ══════════════════════════════════════════════════════════════

        public async Task<OperationResult<BulkImportResultDto>> ImportProducts(BulkProductImportDto dto, IFormFile? imagesZip)
        {
            var companyId = _tenantService.GetCompanyId();
            var semaphore = _importLock.GetLock(companyId);

            if (!await semaphore.WaitAsync(TimeSpan.Zero))
                return OperationResult<BulkImportResultDto>.Conflict(
                    "Ya hay una importación en progreso para esta empresa. Espera a que termine.",
                    ErrorKeys.BulkImportAlreadyInProgress);

            try
            {
                return await ImportProductsInternal(dto, imagesZip, companyId);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task<OperationResult<BulkImportResultDto>> ImportProductsInternal(
            BulkProductImportDto dto, IFormFile? imagesZip, int companyId)
        {
            if (dto.Items.Count > MaxRowsPerImport)
                return OperationResult<BulkImportResultDto>.ValidationError(
                    $"El máximo es {MaxRowsPerImport} filas por importación.",
                    ErrorKeys.BulkImportValidationFailed);

            var companyLangs = await _context.CompanyLanguages
                .Where(cl => cl.CompanyId == companyId)
                .Select(cl => new { cl.LanguageCode, cl.IsDefault })
                .ToListAsync();

            var defaultLang = companyLangs.FirstOrDefault(l => l.IsDefault)?.LanguageCode;
            if (defaultLang is null)
                return OperationResult<BulkImportResultDto>.ValidationError(
                    "La empresa no tiene un idioma predeterminado configurado.",
                    ErrorKeys.BulkImportNoLanguages);

            var validLangs = companyLangs.Select(l => l.LanguageCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var errors = new List<BulkImportRowError>();
            var warnings = new List<BulkImportRowWarning>();

            // V3.6 — Category name → ID
            var categoryNameToId = await _context.CategoryTranslations
                .Where(ct => ct.Category.CompanyId == companyId && ct.LanguageCode == defaultLang)
                .Select(ct => new { Name = ct.Name.Trim().ToLower(), ct.CategoryId })
                .ToDictionaryAsync(x => x.Name, x => x.CategoryId, StringComparer.OrdinalIgnoreCase);

            // V3.7 — Existing product names
            var existingProductNames = await _context.ProductTranslations
                .Where(pt => pt.Product.CompanyId == companyId && pt.LanguageCode == defaultLang)
                .Select(pt => pt.Name.Trim().ToLower())
                .ToListAsync();
            var existingSet = existingProductNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var resolvedCategoryIds = new int[dto.Items.Count];

            for (int i = 0; i < dto.Items.Count; i++)
            {
                var item = dto.Items[i];
                var row = i + 1;

                // Default lang required
                if (!item.Names.TryGetValue(defaultLang, out var defName) || string.IsNullOrWhiteSpace(defName))
                {
                    errors.Add(new BulkImportRowError
                    {
                        Row = row, Field = $"nombre_{defaultLang}",
                        ErrorKey = ErrorKeys.BulkImportDefaultLangRequired,
                        Message = $"El nombre en el idioma predeterminado ({defaultLang}) es obligatorio."
                    });
                    continue;
                }

                var trimmedName = defName.Trim();

                // V3.9 — Length validations
                foreach (var (lang, name) in item.Names)
                {
                    if (!string.IsNullOrWhiteSpace(name) && name.Trim().Length > 150)
                        errors.Add(new BulkImportRowError { Row = row, Field = $"nombre_{lang}", ErrorKey = ErrorKeys.ValidationFailed, Message = "El nombre excede 150 caracteres." });
                }
                foreach (var (lang, desc) in item.ShortDescriptions)
                {
                    if (!string.IsNullOrWhiteSpace(desc) && desc.Trim().Length > 250)
                        errors.Add(new BulkImportRowError { Row = row, Field = $"desc_corta_{lang}", ErrorKey = ErrorKeys.ValidationFailed, Message = "La descripción corta excede 250 caracteres." });
                }
                foreach (var (lang, desc) in item.LongDescriptions)
                {
                    if (!string.IsNullOrWhiteSpace(desc) && desc.Trim().Length > 2000)
                        errors.Add(new BulkImportRowError { Row = row, Field = $"desc_larga_{lang}", ErrorKey = ErrorKeys.ValidationFailed, Message = "La descripción larga excede 2000 caracteres." });
                }

                // V3.6 — Category resolution
                var catName = item.CategoryName.Trim();
                if (!categoryNameToId.TryGetValue(catName, out var categoryId))
                {
                    errors.Add(new BulkImportRowError
                    {
                        Row = row, Field = "categoria",
                        ErrorKey = ErrorKeys.BulkImportCategoryNotFound,
                        Message = $"No se encontró la categoría '{catName}'."
                    });
                }
                else
                {
                    resolvedCategoryIds[i] = categoryId;
                }

                // V3.8 — Duplicate in batch
                if (!seen.Add(trimmedName))
                {
                    errors.Add(new BulkImportRowError
                    {
                        Row = row, Field = $"nombre_{defaultLang}",
                        ErrorKey = ErrorKeys.BulkImportDuplicateRow,
                        Message = $"Nombre duplicado dentro del archivo: '{trimmedName}'."
                    });
                    continue;
                }

                // V3.7 — Duplicate against DB
                if (existingSet.Contains(trimmedName))
                {
                    warnings.Add(new BulkImportRowWarning
                    {
                        Row = row, Field = $"nombre_{defaultLang}",
                        WarningKey = ErrorKeys.BulkImportDuplicateInDb,
                        Message = $"Ya existe un producto con el nombre '{trimmedName}'."
                    });
                }
            }

            if (errors.Count > 0)
                return OperationResult<BulkImportResultDto>.ValidationError(
                    "Se encontraron errores de validación.",
                    ErrorKeys.BulkImportValidationFailed,
                    new BulkImportResultDto { Errors = errors, Warnings = warnings });

            // ── ZIP image processing (outside transaction) ──
            var imageUrlMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var savedImages = new List<(string Url, string Container)>();

            if (imagesZip is not null)
            {
                var zipResult = await ProcessZip(imagesZip, dto.Items.Select(x => x.ImageFilename), "products", warnings);
                if (zipResult.Error is not null)
                    return OperationResult<BulkImportResultDto>.ValidationError(zipResult.Error, ErrorKeys.BulkImportZipInvalid);

                imageUrlMap = zipResult.UrlMap;
                savedImages = zipResult.SavedImages;
            }
            else
            {
                // Warn about referenced images without ZIP
                for (int i = 0; i < dto.Items.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(dto.Items[i].ImageFilename))
                        warnings.Add(new BulkImportRowWarning
                        {
                            Row = i + 1, Field = "imagen",
                            WarningKey = ErrorKeys.BulkImportImageNotFound,
                            Message = "No se subió archivo ZIP. La imagen será ignorada."
                        });
                }
            }

            // ── Transactional insert ──
            var created = 0;
            try
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _context.Database.BeginTransactionAsync();

                    var products = new List<Product>();
                    foreach (var (item, idx) in dto.Items.Select((v, i) => (v, i)))
                    {
                        products.Add(new Product
                        {
                            CompanyId = companyId,
                            CategoryId = resolvedCategoryIds[idx],
                            MainImageUrl = !string.IsNullOrWhiteSpace(item.ImageFilename)
                                ? imageUrlMap.GetValueOrDefault(item.ImageFilename.Trim())
                                : null,
                            ImageObjectFit = "cover",
                            ImageObjectPosition = "50% 50%",
                        });
                    }

                    _context.Products.AddRange(products);
                    await _context.SaveChangesAsync();

                    var translations = new List<ProductTranslation>();
                    foreach (var (product, item) in products.Zip(dto.Items))
                    {
                        foreach (var (lang, name) in item.Names)
                        {
                            if (string.IsNullOrWhiteSpace(name) || !validLangs.Contains(lang)) continue;
                            translations.Add(new ProductTranslation
                            {
                                ProductId = product.Id,
                                LanguageCode = lang.Trim().ToLowerInvariant(),
                                Name = name.Trim(),
                                ShortDescription = item.ShortDescriptions.GetValueOrDefault(lang)?.Trim(),
                                LongDescription = item.LongDescriptions.GetValueOrDefault(lang)?.Trim(),
                            });
                        }
                        created++;
                    }

                    _context.ProductTranslations.AddRange(translations);
                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                });
            }
            catch
            {
                CleanupImages(savedImages);
                throw;
            }

            await _cache.EvictMenuByCompanyAsync(companyId);

            return OperationResult<BulkImportResultDto>.Ok(new BulkImportResultDto
            {
                CreatedCount = created,
                Warnings = warnings
            });
        }

        // ══════════════════════════════════════════════════════════════
        //  IMPORT BRANCH PRODUCTS
        // ══════════════════════════════════════════════════════════════

        public async Task<OperationResult<BulkImportResultDto>> ImportBranchProducts(
            BulkBranchProductImportDto dto, IFormFile? imagesZip)
        {
            var companyId = _tenantService.GetCompanyId();
            var semaphore = _importLock.GetLock(companyId);

            if (!await semaphore.WaitAsync(TimeSpan.Zero))
                return OperationResult<BulkImportResultDto>.Conflict(
                    "Ya hay una importación en progreso para esta empresa. Espera a que termine.",
                    ErrorKeys.BulkImportAlreadyInProgress);

            try
            {
                return await ImportBranchProductsInternal(dto, imagesZip, companyId);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task<OperationResult<BulkImportResultDto>> ImportBranchProductsInternal(
            BulkBranchProductImportDto dto, IFormFile? imagesZip, int companyId)
        {
            if (dto.Items.Count > MaxRowsPerImport)
                return OperationResult<BulkImportResultDto>.ValidationError(
                    $"El máximo es {MaxRowsPerImport} filas por importación.",
                    ErrorKeys.BulkImportValidationFailed);

            var companyLangs = await _context.CompanyLanguages
                .Where(cl => cl.CompanyId == companyId)
                .Select(cl => new { cl.LanguageCode, cl.IsDefault })
                .ToListAsync();

            var defaultLang = companyLangs.FirstOrDefault(l => l.IsDefault)?.LanguageCode;
            if (defaultLang is null)
                return OperationResult<BulkImportResultDto>.ValidationError(
                    "La empresa no tiene un idioma predeterminado configurado.",
                    ErrorKeys.BulkImportNoLanguages);

            var errors = new List<BulkImportRowError>();
            var warnings = new List<BulkImportRowWarning>();

            // V3.10 — Product name → ID
            var productNameToId = await _context.ProductTranslations
                .Where(pt => pt.Product.CompanyId == companyId && pt.LanguageCode == defaultLang)
                .Select(pt => new { Name = pt.Name.Trim().ToLower(), pt.ProductId })
                .ToDictionaryAsync(x => x.Name, x => x.ProductId, StringComparer.OrdinalIgnoreCase);

            // V3.11 — Branch name → ID
            var branchNameToId = await _context.Branches
                .Where(b => b.CompanyId == companyId)
                .Select(b => new { Name = b.Name.Trim().ToLower(), b.Id })
                .ToDictionaryAsync(x => x.Name, x => x.Id, StringComparer.OrdinalIgnoreCase);

            // V3.12 — Category name → ID
            var categoryNameToId = await _context.CategoryTranslations
                .Where(ct => ct.Category.CompanyId == companyId && ct.LanguageCode == defaultLang)
                .Select(ct => new { Name = ct.Name.Trim().ToLower(), ct.CategoryId })
                .ToDictionaryAsync(x => x.Name, x => x.CategoryId, StringComparer.OrdinalIgnoreCase);

            // V3.13 — Validate branch ownership (once per unique branch)
            var uniqueBranchIds = new HashSet<int>();
            foreach (var item in dto.Items)
            {
                if (branchNameToId.TryGetValue(item.BranchName.Trim(), out var bid))
                    uniqueBranchIds.Add(bid);
            }
            foreach (var bid in uniqueBranchIds)
                await _tenantService.ValidateBranchOwnershipAsync(bid);

            // V3.14 — Existing BranchProducts (active + soft-deleted)
            var existingBPs = await _context.BranchProducts
                .IgnoreQueryFilters()
                .Where(bp => bp.Branch.CompanyId == companyId)
                .Select(bp => new { bp.Id, bp.BranchId, bp.ProductId, bp.IsDeleted })
                .ToListAsync();

            var activeBPSet = existingBPs
                .Where(x => !x.IsDeleted)
                .ToDictionary(x => $"{x.BranchId}:{x.ProductId}", x => x.Id, StringComparer.Ordinal);

            var deletedBPSet = existingBPs
                .Where(x => x.IsDeleted)
                .ToDictionary(x => $"{x.BranchId}:{x.ProductId}", x => x.Id, StringComparer.Ordinal);

            // Per-item resolved data
            var resolvedItems = new List<(int ProductId, int BranchId, int CategoryId, BulkBranchProductImportItemDto Item, int? ReactivateId)>();
            var seenBP = new HashSet<string>(StringComparer.Ordinal);
            var skippedCount = 0;

            for (int i = 0; i < dto.Items.Count; i++)
            {
                var item = dto.Items[i];
                var row = i + 1;

                // Resolve names to IDs
                if (!productNameToId.TryGetValue(item.ProductName.Trim(), out var productId))
                {
                    errors.Add(new BulkImportRowError { Row = row, Field = "producto", ErrorKey = ErrorKeys.BulkImportProductNotFound, Message = $"No se encontró el producto '{item.ProductName.Trim()}'." });
                    continue;
                }
                if (!branchNameToId.TryGetValue(item.BranchName.Trim(), out var branchId))
                {
                    errors.Add(new BulkImportRowError { Row = row, Field = "sucursal", ErrorKey = ErrorKeys.BulkImportBranchNotFound, Message = $"No se encontró la sucursal '{item.BranchName.Trim()}'." });
                    continue;
                }
                if (!categoryNameToId.TryGetValue(item.CategoryName.Trim(), out var categoryId))
                {
                    errors.Add(new BulkImportRowError { Row = row, Field = "categoria", ErrorKey = ErrorKeys.BulkImportCategoryNotFound, Message = $"No se encontró la categoría '{item.CategoryName.Trim()}'." });
                    continue;
                }

                // V3.16 — Price validation
                if (item.Price < 0 || item.Price > 9999999.99m)
                {
                    errors.Add(new BulkImportRowError { Row = row, Field = "precio", ErrorKey = ErrorKeys.BulkImportInvalidPrice, Message = "El precio debe estar entre 0 y 9,999,999.99." });
                    continue;
                }
                if (item.OfferPrice is not null)
                {
                    if (item.OfferPrice < 0 || item.OfferPrice > 9999999.99m)
                        errors.Add(new BulkImportRowError { Row = row, Field = "precio_oferta", ErrorKey = ErrorKeys.BulkImportInvalidPrice, Message = "El precio de oferta debe estar entre 0 y 9,999,999.99." });
                    else if (item.OfferPrice >= item.Price)
                        errors.Add(new BulkImportRowError { Row = row, Field = "precio_oferta", ErrorKey = ErrorKeys.BulkImportInvalidPrice, Message = "El precio de oferta debe ser menor al precio base." });
                }

                // V3.15 — Duplicate in batch
                var key = $"{branchId}:{productId}";
                if (!seenBP.Add(key))
                {
                    errors.Add(new BulkImportRowError { Row = row, Field = "producto+sucursal", ErrorKey = ErrorKeys.BulkImportDuplicateRow, Message = $"Combinación producto-sucursal duplicada en el archivo." });
                    continue;
                }

                // V3.14 — Check existing BranchProducts
                int? reactivateId = null;
                if (activeBPSet.ContainsKey(key))
                {
                    // Already active → warn and skip. Cannot insert duplicate (BranchId, ProductId).
                    // Frontend pre-activates "discard" for these rows; if user force-sends it anyway, skip it.
                    warnings.Add(new BulkImportRowWarning
                    {
                        Row = row, Field = "producto+sucursal",
                        WarningKey = ErrorKeys.BulkImportBranchProductExists,
                        Message = $"El producto '{item.ProductName.Trim()}' ya está activo en '{item.BranchName.Trim()}'. Se omitió."
                    });
                    skippedCount++;
                    continue;
                }
                else if (deletedBPSet.TryGetValue(key, out var deletedId))
                {
                    reactivateId = deletedId;
                }

                resolvedItems.Add((productId, branchId, categoryId, item, reactivateId));
            }

            if (errors.Count > 0)
                return OperationResult<BulkImportResultDto>.ValidationError(
                    "Se encontraron errores de validación.",
                    ErrorKeys.BulkImportValidationFailed,
                    new BulkImportResultDto { Errors = errors, Warnings = warnings });

            // ── ZIP processing ──
            var imageUrlMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var savedImages = new List<(string Url, string Container)>();

            if (imagesZip is not null)
            {
                var zipResult = await ProcessZip(imagesZip, dto.Items.Select(x => x.ImageFilename), "branch-products", warnings);
                if (zipResult.Error is not null)
                    return OperationResult<BulkImportResultDto>.ValidationError(zipResult.Error, ErrorKeys.BulkImportZipInvalid);

                imageUrlMap = zipResult.UrlMap;
                savedImages = zipResult.SavedImages;
            }

            // ── Transactional insert ──
            var created = 0;
            var reactivated = 0;

            try
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _context.Database.BeginTransactionAsync();

                    var newBPs = new List<BranchProduct>();

                    foreach (var (productId, branchId, categoryId, item, reactivateId) in resolvedItems)
                    {
                        var imageUrl = !string.IsNullOrWhiteSpace(item.ImageFilename)
                            ? imageUrlMap.GetValueOrDefault(item.ImageFilename.Trim())
                            : null;

                        if (reactivateId.HasValue)
                        {
                            // Re-activate soft-deleted BranchProduct
                            var bp = await _context.BranchProducts
                                .IgnoreQueryFilters()
                                .FirstAsync(bp => bp.Id == reactivateId.Value);

                            bp.IsDeleted = false;
                            bp.Price = item.Price;
                            bp.OfferPrice = item.OfferPrice;
                            bp.CategoryId = categoryId;
                            bp.IsVisible = item.IsVisible;
                            bp.ImageOverrideUrl = imageUrl;
                            bp.ImageObjectFit = "cover";
                            bp.ImageObjectPosition = "50% 50%";
                            reactivated++;
                        }
                        else
                        {
                            newBPs.Add(new BranchProduct
                            {
                                BranchId = branchId,
                                ProductId = productId,
                                CategoryId = categoryId,
                                Price = item.Price,
                                OfferPrice = item.OfferPrice,
                                IsVisible = item.IsVisible,
                                ImageOverrideUrl = imageUrl,
                                ImageObjectFit = "cover",
                                ImageObjectPosition = "50% 50%",
                                DisplayOrder = 0,
                            });
                        }
                    }

                    if (newBPs.Count > 0)
                    {
                        _context.BranchProducts.AddRange(newBPs);
                        created = newBPs.Count;
                    }

                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                });
            }
            catch
            {
                CleanupImages(savedImages);
                throw;
            }

            await _cache.EvictMenuByCompanyAsync(companyId);

            return OperationResult<BulkImportResultDto>.Ok(new BulkImportResultDto
            {
                CreatedCount = created,
                ReactivatedCount = reactivated,
                SkippedCount = skippedCount,
                Warnings = warnings
            });
        }

        // ══════════════════════════════════════════════════════════════
        //  ZIP PROCESSING (shared)
        // ══════════════════════════════════════════════════════════════

        private record ZipProcessResult(
            string? Error,
            Dictionary<string, string> UrlMap,
            List<(string Url, string Container)> SavedImages);

        private async Task<ZipProcessResult> ProcessZip(
            IFormFile zipFile,
            IEnumerable<string?> referencedFilenames,
            string container,
            List<BulkImportRowWarning> warnings)
        {
            var urlMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var savedImages = new List<(string Url, string Container)>();

            // V-ZIP.1 — Size limit
            if (zipFile.Length > MaxZipSize)
                return new ZipProcessResult($"El archivo ZIP excede el límite de {MaxZipSize / (1024 * 1024)} MB.", urlMap, savedImages);

            ZipArchive archive;
            try
            {
                archive = new ZipArchive(zipFile.OpenReadStream(), ZipArchiveMode.Read);
            }
            catch (InvalidDataException)
            {
                return new ZipProcessResult("El archivo ZIP está corrupto o no es válido.", urlMap, savedImages);
            }

            using (archive)
            {
                // V-ZIP.6 — Zip bomb protection
                long totalUncompressed = archive.Entries.Sum(e => e.Length);
                if (totalUncompressed > MaxUncompressedSize)
                    return new ZipProcessResult($"El contenido descomprimido excede {MaxUncompressedSize / (1024 * 1024)} MB.", urlMap, savedImages);

                // V-ZIP.3 + V-ZIP.5 — Validate entries
                var validEntries = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);

                foreach (var entry in archive.Entries)
                {
                    if (entry.Length == 0) continue;

                    var safeName = Path.GetFileName(entry.FullName);
                    if (string.IsNullOrWhiteSpace(safeName)) continue;

                    var ext = Path.GetExtension(safeName).ToLowerInvariant();

                    // Reject dangerous files immediately
                    if (DangerousExtensions.Contains(ext))
                        return new ZipProcessResult($"El archivo '{safeName}' es un tipo de archivo peligroso y no se permite.", urlMap, savedImages);

                    if (!AllowedImageExtensions.Contains(ext))
                    {
                        warnings.Add(new BulkImportRowWarning
                        {
                            Row = 0, Field = "zip",
                            WarningKey = ErrorKeys.BulkImportZipInvalidFile,
                            Message = $"'{safeName}' no es una imagen válida y será ignorado."
                        });
                        continue;
                    }

                    // V-ZIP.4 — Individual size
                    if (entry.Length > MaxImageSize)
                    {
                        warnings.Add(new BulkImportRowWarning
                        {
                            Row = 0, Field = "zip",
                            WarningKey = ErrorKeys.BulkImportZipInvalidFile,
                            Message = $"La imagen '{safeName}' excede 5 MB y será ignorada."
                        });
                        continue;
                    }

                    validEntries.TryAdd(safeName, entry);
                }

                // Process only referenced images
                var needed = referencedFilenames
                    .Where(f => !string.IsNullOrWhiteSpace(f))
                    .Select(f => f!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase);

                foreach (var filename in needed)
                {
                    if (!validEntries.TryGetValue(filename, out var entry))
                    {
                        // V-ZIP.7 — Image not found in ZIP → warning (non-blocking), record will be created without image
                        warnings.Add(new BulkImportRowWarning
                        {
                            Row = 0, Field = "imagen",
                            WarningKey = ErrorKeys.BulkImportImageNotFound,
                            Message = $"La imagen '{filename}' no se encontró en el ZIP. El registro se creará sin imagen."
                        });
                        continue;
                    }

                    using var stream = entry.Open();
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    ms.Position = 0;

                    var formFile = new FormFile(ms, 0, ms.Length, "image", Path.GetFileName(entry.FullName));
                    var url = await _fileStorage.SaveFile(formFile, container);
                    urlMap[filename] = url;
                    savedImages.Add((url, container));
                }
            }

            return new ZipProcessResult(null, urlMap, savedImages);
        }

        // ══════════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════════

        private async Task<(List<string>? Langs, string? Error)> GetCompanyLanguages()
        {
            var companyId = _tenantService.GetCompanyId();
            var languages = await _context.CompanyLanguages
                .Where(cl => cl.CompanyId == companyId)
                .OrderByDescending(cl => cl.IsDefault)
                .ThenBy(cl => cl.LanguageCode)
                .Select(cl => cl.LanguageCode)
                .ToListAsync();

            return languages.Count == 0
                ? (null, "La empresa no tiene idiomas configurados.")
                : (languages, null);
        }

        private void CleanupImages(List<(string Url, string Container)> savedImages)
        {
            foreach (var (url, container) in savedImages)
            {
                try { _fileStorage.DeleteFile(url, container); }
                catch { /* best-effort cleanup */ }
            }
        }
    }
}
