namespace DigiMenuAPI.Application.DTOs.Create
{
    public record TagTranslationCreateDto(
        int TagId,
        string LanguageCode,
        string Name
    );
}
