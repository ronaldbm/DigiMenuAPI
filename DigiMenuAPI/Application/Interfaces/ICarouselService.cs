using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Read;

namespace DigiMenuAPI.Application.Interfaces
{
    /// <summary>
    /// Agrega eventos activos + promociones activas en un único array
    /// para el carrusel del menú público.
    /// </summary>
    public interface ICarouselService
    {
        Task<OperationResult<List<CarouselItemDto>>> GetCarouselItems(
            string companySlug, string branchSlug);
    }
}
