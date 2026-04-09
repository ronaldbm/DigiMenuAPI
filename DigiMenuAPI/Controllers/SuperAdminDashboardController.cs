using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    /// <summary>
    /// Métricas globales de la plataforma para el dashboard del SuperAdmin.
    /// Requiere role=255 (SuperAdmin).
    /// </summary>
    [Route("api/superadmin/dashboard")]
    [Authorize(Policy = "SuperAdminOnly")]
    public class SuperAdminDashboardController : BaseController
    {
        private readonly ISuperAdminDashboardService _service;

        public SuperAdminDashboardController(ISuperAdminDashboardService service)
            => _service = service;

        /// <summary>Retorna todas las métricas del dashboard en una sola llamada.</summary>
        [HttpGet("metrics")]
        public async Task<ActionResult> GetMetrics()
            => HandleResult(await _service.GetMetrics());
    }
}
