using AppCore.Application.Common;
using AppCore.Filters;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.Interfaces;
using AppCore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    [Route("api/[controller]")]
    public class ReservationsController : BaseController
    {
        private readonly IReservationService _service;
        private readonly ITenantService _tenantService;

        public ReservationsController(
            IReservationService service,
            ITenantService tenantService)
        {
            _service = service;
            _tenantService = tenantService;
        }

        /// <summary>Admin: ver reservas (CompanyAdmin = todas las branches, BranchAdmin = solo la suya).</summary>
        [HttpGet]
        [Authorize]
        [RequireModule(ModuleCodes.Reservations)]
        public async Task<ActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
            => HandleResult(await _service.GetAll(page, pageSize));

        /// <summary>
        /// Público: el cliente hace una reserva en la Branch identificada por companySlug + branchSlug.
        /// POST /api/reservations/el-rancho/centro
        /// Branch.Slug es único dentro de la Company — se necesitan ambos slugs.
        /// </summary>
        [HttpPost("{companySlug}/{branchSlug}")]
        [AllowAnonymous]
        public async Task<ActionResult> Create(
            string companySlug,
            string branchSlug,
            [FromBody] ReservationCreateDto dto)
        {
            var (branchId, companyId) = await _tenantService
                .ResolveBySlugAsync(companySlug, branchSlug);

            if (branchId is null || companyId is null)
                return NotFound(new { Success = false, Message = "Sucursal no encontrada." });

            return HandleResult(await _service.Create(dto, branchId.Value, companyId.Value));
        }

        /// <summary>Admin: cambiar estado de una reserva.</summary>
        [HttpPatch("{id:int}/status")]
        [Authorize]
        [RequireModule(ModuleCodes.Reservations)]
        public async Task<ActionResult> UpdateStatus(int id, [FromBody] ReservationStatus status)
            => HandleResult(await _service.UpdateStatus(id, status));

        /// <summary>Admin: eliminar reserva (soft delete).</summary>
        [HttpDelete("{id:int}")]
        [Authorize]
        [RequireModule(ModuleCodes.Reservations)]
        public async Task<ActionResult> Delete(int id)
            => HandleResult(await _service.Delete(id));
    }
}