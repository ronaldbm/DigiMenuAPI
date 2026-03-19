namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>Traducción de nombre simple (Category, Tag).</summary>
    public class TranslationReadDto
    {
        public int Id { get; init; }
        public string LanguageCode { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
    }

    /// <summary>Traducción completa de un producto (nombre + descripciones).</summary>
    public class ProductTranslationReadDto
    {
        public int Id { get; init; }
        public string LanguageCode { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? ShortDescription { get; init; }
        public string? LongDescription { get; init; }
    }
}