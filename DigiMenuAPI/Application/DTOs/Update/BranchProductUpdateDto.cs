using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Update
{
    public record BranchProductUpdateDto(
        int Id,
        [Range(1, int.MaxValue)] int CategoryId,
        [Range(0, 9999999.99)] decimal Price,
        [Range(0, 9999999.99)] decimal? OfferPrice,
        IFormFile? ImageOverride,
        bool IsVisible,
        string? ImageObjectFit = null,
        string? ImageObjectPosition = null
    );
}
