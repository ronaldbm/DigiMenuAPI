using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    /// <summary>
    /// Estadísticas agregadas para el panel de administración.
    ///   GET /api/dashboard/stats → Conteos de productos, categorías, tags y usuarios
    /// </summary>
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : BaseController
    {
        private readonly IDashboardService _service;

        public DashboardController(IDashboardService service)
        {
            _service = service;
        }

        /// <summary>Devuelve los conteos del catálogo en una sola llamada.</summary>
        [HttpGet("stats")]
        public async Task<ActionResult> GetStats()
            => HandleResult(await _service.GetStats());
    }
}
