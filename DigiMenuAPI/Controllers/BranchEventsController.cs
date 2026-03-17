using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    /// <summary>
    /// Gestión de eventos promocionales de una sucursal.
    ///
    /// Admin (requiere JWT):
    ///   GET    /api/events/{branchId}/all   → Lista todos los eventos de la sucursal
    ///   GET    /api/events/{id}             → Obtiene un evento por Id
    ///   POST   /api/events                  → Crea un evento (multipart/form-data)
    ///   PUT    /api/events/{id}             → Actualiza un evento (multipart/form-data)
    ///   DELETE /api/events/{id}             → Elimina un evento
    ///
    /// Público (sin JWT):
    ///   GET    /api/events/public/{companySlug}/{branchSlug} → Próximos eventos activos
    /// </summary>
    [Route("api/events")]
    public class BranchEventsController : BaseController
    {
        private readonly IBranchEventService _service;

        public BranchEventsController(IBranchEventService service)
        {
            _service = service;
        }

        // ── Admin ──────────────────────────────────────────────────────

        [HttpGet("{branchId:int}/all")]
        [Authorize]
        public async Task<ActionResult> GetAll(int branchId)
            => HandleResult(await _service.GetEvents(branchId));

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<ActionResult> GetById(int id)
            => HandleResult(await _service.GetById(id));

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> Create([FromForm] BranchEventCreateDto dto)
            => HandleResult(await _service.Create(dto));

        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<ActionResult> Update(int id, [FromForm] BranchEventUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest(new { Success = false, Message = "El Id de la ruta no coincide con el del body." });

            return HandleResult(await _service.Update(dto));
        }

        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<ActionResult> Delete(int id)
            => HandleResult(await _service.Delete(id));

        // ── Público ────────────────────────────────────────────────────

        [HttpGet("public/{companySlug}/{branchSlug}")]
        [AllowAnonymous]
        public async Task<ActionResult> GetUpcoming(string companySlug, string branchSlug)
            => HandleResult(await _service.GetUpcomingEvents(companySlug, branchSlug));

        [HttpGet("public/{companySlug}/{branchSlug}/announcement")]
        [AllowAnonymous]
        public async Task<ActionResult> GetNextAnnouncement(string companySlug, string branchSlug)
            => HandleResult(await _service.GetNextAnnouncement(companySlug, branchSlug));
    }
}
