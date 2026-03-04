namespace DigiMenuAPI.Application.DTOs.Create
{
    public record CategoryTranslationCreateDto(
        int CategoryId,
        string LanguageCode,
        string Name
    );
}
