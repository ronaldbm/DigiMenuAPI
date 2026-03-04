namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>Traducción de nombre simple (Category, Tag).</summary>
    public record TranslationReadDto(
        int Id,
        string LanguageCode,
        string Name
    );

    /// <summary>Traducción completa de un producto (nombre + descripciones).</summary>
    public record ProductTranslationReadDto(
        int Id,
        string LanguageCode,
        string Name,
        string? ShortDescription,
        string? LongDescription
    );
}