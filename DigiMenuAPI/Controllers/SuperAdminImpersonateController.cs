using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DigiMenuAPI.Controllers
{
    /// <summary>
    /// Emisión de tokens de impersonación para acceso de soporte técnico a tenants.
    ///
    /// Seguridad:
    ///   - Crear token: requiere role=255 (SuperAdmin)
    ///   - Exchange: público (necesario porque DigiMenuWeb no tiene JWT de SuperAdmin),
    ///     pero con rate limiting estricto y validación de hash one-time-use
    /// </summary>
    [Route("api/superadmin")]
    public class SuperAdminImpersonateController : BaseController
    {
        private readonly ISuperAdminImpersonationService _service;

        public SuperAdminImpersonateController(ISuperAdminImpersonationService service)
            => _service = service;

        /// <summary>
        /// Genera un token de impersonación (30 min, un solo uso) para el tenant indicado.
        /// Requiere role=255 (SuperAdmin).
        /// </summary>
        [HttpPost("impersonate/{companyId:int}")]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<ActionResult> CreateToken(int companyId)
            => HandleResult(await _service.CreateToken(companyId));
    }
}
