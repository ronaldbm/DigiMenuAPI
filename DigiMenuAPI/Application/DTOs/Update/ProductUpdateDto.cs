namespace DigiMenuAPI.Application.DTOs.Update
{
    public record ProductUpdateDto(
        int Id,
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
