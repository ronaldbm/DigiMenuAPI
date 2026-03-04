namespace DigiMenuAPI.Application.DTOs.Update
{
    public record ProductUpdateDto(
        int Id,
        int CategoryId,
        string Name,
        string? ShortDescription,
        string? LongDescription,
        IFormFile? Image,
        List<int>? TagIds
    );
}
