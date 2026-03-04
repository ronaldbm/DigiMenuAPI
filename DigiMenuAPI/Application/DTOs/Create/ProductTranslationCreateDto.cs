namespace DigiMenuAPI.Application.DTOs.Create
{
    public record ProductTranslationCreateDto(
        int ProductId,
        string LanguageCode,
        string Name,
        string? ShortDescription,
        string? LongDescription
    );
}
