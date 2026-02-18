namespace DigiMenuAPI.Application.DTOs.UpdateDTOs
{
    public record ProductUpdateDto(
        int Id,
        string Name,
        string? ShortDescription,
        string? LongDescription,
        decimal Price,
        decimal? OfferPrice,
        int DisplayOrder,
        int CategoryId,
        bool IsVisible,
        string? Image,
        List<int> TagIds
    );
}
