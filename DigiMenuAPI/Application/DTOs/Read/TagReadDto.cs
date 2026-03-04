namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>Tag para el panel admin (incluye traducciones).</summary>
    public record TagReadDto(
        int Id,
        int CompanyId,
        string Name,
        string? Color,
        List<TranslationReadDto> Translations
    );

    /// <summary>
    /// Tag para el menú público.
    /// El nombre ya viene resuelto al idioma solicitado.
    /// </summary>
    public record TagMenuDto(
        int Id,
        string Name,
        string? Color
    );
}