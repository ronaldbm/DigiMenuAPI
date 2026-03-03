using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.Add;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

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

        /// <summary>Admin: ver todas las reservas de su empresa.</summary>
        [HttpGet]
        [Authorize]
        [RequireModule(ModuleCodes.Reservations)]
        public async Task<ActionResult> GetAll()
            => HandleResult(await _service.GetAll());

        /// <summary>
        /// Público: el cliente hace una reserva.
        /// Se envía el slug para resolver la empresa.
        /// Módulo verificado internamente en el servicio.
        /// </summary>
        [HttpPost("{slug}")]
        [AllowAnonymous]
        public async Task<ActionResult> Create(string slug, [FromBody] ReservationCreateDto dto)
        {
            // Resolver empresa por slug
            var companyId = await _tenantService.ResolveCompanyBySlugAsync(slug);
            if (companyId is null)
                return NotFound(new { Success = false, Message = "Empresa no encontrada." });

            return HandleResult(await _service.Create(dto, companyId.Value));
        }

        /// <summary>Admin: cambiar estado de reserva.</summary>
        [HttpPatch("{id:int}/status")]
        [Authorize]
        [RequireModule(ModuleCodes.Reservations)]
        public async Task<ActionResult> UpdateStatus(int id, [FromBody] byte status)
            => HandleResult(await _service.UpdateStatus(id, status));

        /// <summary>Admin: eliminar reserva (soft delete).</summary>
        [HttpDelete("{id:int}")]
        [Authorize]
        [RequireModule(ModuleCodes.Reservations)]
        public async Task<ActionResult> Delete(int id)
            => HandleResult(await _service.Delete(id));
    }
}