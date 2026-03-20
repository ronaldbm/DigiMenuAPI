using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DigiMenuAPI.Controllers
{
    /// <summary>
    /// Endpoint público del carrusel — mezcla eventos + promociones activas.
    ///
    ///   GET /api/carousel/public/{companySlug}/{branchSlug}
    ///     → Devuelve array de CarouselItemDto (vacío si no hay items).
    ///     → Eventos primero (por fecha ASC), luego promos (por DisplayOrder ASC).
    /// </summary>
    [Route("api/carousel")]
    public class CarouselController : BaseController
    {
        private readonly ICarouselService _service;

        public CarouselController(ICarouselService service)
        {
            _service = service;
        }

        [HttpGet("public/{companySlug}/{branchSlug}")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult> GetCarouselItems(string companySlug, string branchSlug)
            => HandleResult(await _service.GetCarouselItems(companySlug, branchSlug));
    }
}
