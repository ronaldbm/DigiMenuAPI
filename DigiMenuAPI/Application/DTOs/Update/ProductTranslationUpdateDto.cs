namespace DigiMenuAPI.Application.DTOs.Update
{
    public record ProductTranslationUpdateDto(
        int Id,
        string Name,
        string? ShortDescription,
        string? LongDescription
    );
}
