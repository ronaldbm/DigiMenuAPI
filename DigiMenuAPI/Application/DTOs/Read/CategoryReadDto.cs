namespace DigiMenuAPI.Application.DTOs.Read
{
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