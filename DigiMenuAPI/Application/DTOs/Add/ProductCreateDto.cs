namespace DigiMenuAPI.Application.DTOs.Add
{
    public record ProductCreateDto(
        string Name,
        string? ShortDescription,
        string? LongDescription,
        decimal BasePrice,
        decimal? OfferPrice,
        int DisplayOrder,
        int CategoryId,
        bool IsVisible,
        IFormFile? Image,
        List<int>? TagIds
    );
}
