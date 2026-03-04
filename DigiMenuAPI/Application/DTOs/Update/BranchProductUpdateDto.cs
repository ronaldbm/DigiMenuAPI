namespace DigiMenuAPI.Application.DTOs.Update
{
    public record BranchProductUpdateDto(
        int Id,
        int CategoryId,
        decimal Price,
        decimal? OfferPrice,
        IFormFile? ImageOverride,
        int DisplayOrder,
        bool IsVisible
    );
}
