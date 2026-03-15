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
    public record CategoryReadDto(
        int Id,
        int CompanyId,
        string Name,
        int DisplayOrder,
        bool IsVisible,
        List<TranslationReadDto> Translations
    );

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