namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>Tag para el panel admin (incluye traducciones).</summary>
    public class TagReadDto
    {
        public int Id { get; init; }
        public int CompanyId { get; init; }
        public string? Color { get; init; }
        public List<TranslationReadDto> Translations { get; init; } = [];
    }

    /// <summary>
    /// Tag para listados del panel admin.
    /// El nombre ya viene resuelto al idioma solicitado (con fallback).
    /// </summary>
    public class TagListItemDto
    {
        public int Id { get; init; }
        public int CompanyId { get; init; }
        public string? Color { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    /// <summary>
    /// Tag compacto para el tooltip lazy de la lista de productos.
    /// Solo incluye los campos necesarios para renderizar el chip.
    /// </summary>
    public record TagTooltipDto(string Name, string? Color);

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