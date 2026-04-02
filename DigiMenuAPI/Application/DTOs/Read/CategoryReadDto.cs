namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>
    /// Visibilidad de una categoría dentro de una Branch específica.
    /// </summary>
    public record BranchCategoryVisibilityDto(
        int CategoryId,
        string CategoryName,
        int DisplayOrder,
        bool AnyVisible,
        int TotalProducts,
        int VisibleProducts
    );

    /// <summary>Categoría para el panel admin (incluye traducciones y campos de apariencia).</summary>
    public class CategoryReadDto
    {
        public int Id { get; init; }
        public int CompanyId { get; init; }
        public int DisplayOrder { get; init; }
        public bool IsVisible { get; init; }
        public string? HeaderImageUrl { get; init; }
        public byte? HeaderStyleOverride { get; init; }
        public List<TranslationReadDto> Translations { get; init; } = [];
    }

    /// <summary>Categoría para listados del panel admin.</summary>
    public class CategoryListItemDto
    {
        public int Id { get; init; }
        public int CompanyId { get; init; }
        public int DisplayOrder { get; init; }
        public bool IsVisible { get; init; }
        public string? HeaderImageUrl { get; init; }
        public byte? HeaderStyleOverride { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    /// <summary>
    /// Categoría para el menú público de una Branch.
    /// HeaderImageUrl será null si ShowCategoryImages=false en CompanyTheme.
    /// </summary>
    public record CategoryMenuDto(
        int Id,
        string Name,
        int DisplayOrder,
        string? HeaderImageUrl,
        byte? HeaderStyleOverride,
        List<BranchProductMenuDto> Products
    );
}
