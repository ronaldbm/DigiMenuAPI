namespace DigiMenuAPI.Application.DTOs.AddDTOs
{
    public record ProductCreateDto(
        string Name,
        string? ShortDescription,
        string? LongDescription,
        decimal Price,
        decimal? OfferPrice,
        int DisplayOrder,
        int CategoryId,
        bool IsVisible,
        string? Image, // Recibe el Base64 desde Angular
        List<int> TagIds
    );
}
