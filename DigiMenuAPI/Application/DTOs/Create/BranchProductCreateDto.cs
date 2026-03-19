using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Create
{
    /// <summary>
    /// Activa un producto del catálogo global en una Branch con su precio y configuración.
    /// </summary>
    public record BranchProductCreateDto(
        [Range(1, int.MaxValue)] int BranchId,
        [Range(1, int.MaxValue)] int ProductId,
        [Range(1, int.MaxValue)] int CategoryId,
        [Range(0, 9999999.99)] decimal Price,
        [Range(0, 9999999.99)] decimal? OfferPrice,
        IFormFile? ImageOverride,
        int DisplayOrder,
        bool IsVisible
    );
}
