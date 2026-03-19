namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>
    /// Visibilidad de una categoría dentro de una Branch específica.
    /// Se calcula a partir de los BranchProducts activos de esa categoría en esa sucursal.
    /// AnyVisible = true si al menos un producto está visible.
    /// </summary>
    public record BranchCategoryVisibilityDto(
        int CategoryId,
        string CategoryName,
        int DisplayOrder,
        bool AnyVisible,
        int TotalProducts,
        int VisibleProducts
    );

    /// <summary>Categoría para el panel admin (incluye traducciones).</summary>
    public class CategoryReadDto
    {
        public int Id { get; init; }
        public int CompanyId { get; init; }
        public int DisplayOrder { get; init; }
        public bool IsVisible { get; init; }
        public List<TranslationReadDto> Translations { get; init; } = [];
    }

    /// <summary>
    /// Categoría para listados del panel admin.
    /// El nombre ya viene resuelto al idioma solicitado (con fallback).
    /// </summary>
    public class CategoryListItemDto
    {
        public int Id { get; init; }
        public int CompanyId { get; init; }
        public int DisplayOrder { get; init; }
        public bool IsVisible { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    /// <summary>
    /// Categoría para el menú público de una Branch.
    /// El nombre ya viene resuelto al idioma solicitado (con fallback al base).
    /// </summary>
    public record CategoryMenuDto(
        int Id,
        string Name,
        int DisplayOrder,
        List<BranchProductMenuDto> Products
    );
}