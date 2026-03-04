namespace DigiMenuAPI.Application.DTOs.Create
{
    /// <summary>
    /// Activa un producto del catálogo global en una Branch con su precio y configuración.
    /// </summary>
    public record BranchProductCreateDto(
        int BranchId,
        int ProductId,
        int CategoryId,
        decimal Price,
        decimal? OfferPrice,
        IFormFile? ImageOverride,
        int DisplayOrder,
        bool IsVisible
    );
}
