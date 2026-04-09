using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Create
{
    // ── Category Bulk Import ──────────────────────────────────────────

    public class BulkCategoryImportItemDto
    {
        /// <summary>LanguageCode → Name (e.g. "es" → "Entradas")</summary>
        [Required, MinLength(1)]
        public Dictionary<string, string> Names { get; init; } = new();

        public bool IsVisible { get; init; } = true;
    }

    public class BulkCategoryImportDto
    {
        [Required, MinLength(1)]
        public List<BulkCategoryImportItemDto> Items { get; init; } = [];
    }

    // ── Product Bulk Import ───────────────────────────────────────────

    public class BulkProductImportItemDto
    {
        /// <summary>Category name in default language — resolved to CategoryId by service.</summary>
        [Required, MaxLength(100)]
        public string CategoryName { get; init; } = string.Empty;

        /// <summary>LanguageCode → Product name</summary>
        [Required, MinLength(1)]
        public Dictionary<string, string> Names { get; init; } = new();

        /// <summary>LanguageCode → Short description (optional per language)</summary>
        public Dictionary<string, string?> ShortDescriptions { get; init; } = new();

        /// <summary>LanguageCode → Long description (optional per language)</summary>
        public Dictionary<string, string?> LongDescriptions { get; init; } = new();

        /// <summary>Filename inside the ZIP (optional — null means no image).</summary>
        public string? ImageFilename { get; init; }
    }

    public class BulkProductImportDto
    {
        [Required, MinLength(1)]
        public List<BulkProductImportItemDto> Items { get; init; } = [];
    }

    // ── BranchProduct Bulk Import ─────────────────────────────────────

    public class BulkBranchProductImportItemDto
    {
        /// <summary>Product name in default language — resolved to ProductId by service.</summary>
        [Required, MaxLength(150)]
        public string ProductName { get; init; } = string.Empty;

        /// <summary>Branch.Name — resolved to BranchId by service.</summary>
        [Required, MaxLength(100)]
        public string BranchName { get; init; } = string.Empty;

        /// <summary>Category name in default language — resolved to CategoryId by service.</summary>
        [Required, MaxLength(100)]
        public string CategoryName { get; init; } = string.Empty;

        [Range(0, 9999999.99)]
        public decimal Price { get; init; }

        [Range(0, 9999999.99)]
        public decimal? OfferPrice { get; init; }

        public bool IsVisible { get; init; } = true;

        /// <summary>Filename inside the ZIP (optional).</summary>
        public string? ImageFilename { get; init; }
    }

    public class BulkBranchProductImportDto
    {
        [Required, MinLength(1)]
        public List<BulkBranchProductImportItemDto> Items { get; init; } = [];
    }
}
