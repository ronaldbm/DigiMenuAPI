using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    /// <summary>
    /// Gestión del horario semanal y días especiales de una Branch.
    /// Independiente del módulo RESERVATIONS — es información del negocio.
    ///
    /// Horario semanal:
    ///   GET   /api/schedule/{branchId}          → 7 días ordenados Lun-Dom
    ///   PATCH /api/schedule/{branchId}/day      → Actualizar un día
    ///
    /// Días especiales:
    ///   GET    /api/schedule/{branchId}/special-days        → Listar (incluye historial)
    ///   POST   /api/schedule/{branchId}/special-days        → Crear
    ///   PUT    /api/schedule/{branchId}/special-days/{id}   → Editar
    ///   DELETE /api/schedule/{branchId}/special-days/{id}   → Eliminar (físico)
    /// </summary>
    [Route("api/schedule")]
    [Authorize]
    public class ScheduleController : BaseController
    {
        private readonly IScheduleService _service;

        public ScheduleController(IScheduleService service)
        {
            _service = service;
        }

        /// <summary>Devuelve los 7 días del horario semanal ordenados Lun-Dom.</summary>
        [HttpGet("{branchId:int}")]
        public async Task<ActionResult> GetSchedule(int branchId)
            => HandleResult(await _service.GetSchedule(branchId));

        /// <summary>
        /// Actualiza el horario de un día específico.
        /// Si IsOpen = false, los horarios se guardan como null.
        /// </summary>
        [HttpPatch("{branchId:int}/day")]
        public async Task<ActionResult> UpdateScheduleDay(
            int branchId, [FromBody] BranchScheduleUpdateDto dto)
        {
            if (branchId != dto.BranchId)
                return BadRequest(new { Success = false, Message = "BranchId inconsistente." });

            return HandleResult(await _service.UpdateScheduleDay(dto));
        }

        /// <summary>
        /// Actualiza los 7 días del horario semanal en una sola operación.
        /// </summary>
        [HttpPut("{branchId:int}/week")]
        public async Task<ActionResult> UpdateScheduleWeek(
            int branchId, [FromBody] List<BranchScheduleUpdateDto> items)
        {
            if (items.Any(d => d.BranchId != branchId))
                return BadRequest(new { Success = false, Message = "BranchId inconsistente en uno o más días." });

            return HandleResult(await _service.UpdateScheduleWeek(branchId, items));
        }

        /// <summary>
        /// Lista todos los días especiales ordenados por fecha.
        /// Incluye fechas pasadas para historial.
        /// </summary>
        [HttpGet("{branchId:int}/special-days")]
        public async Task<ActionResult> GetSpecialDays(int branchId)
            => HandleResult(await _service.GetSpecialDays(branchId));

        /// <summary>
        /// Crea un día especial (feriado, cierre excepcional, horario diferente).
        /// La fecha no puede ser pasada.
        /// </summary>
        [HttpPost("{branchId:int}/special-days")]
        public async Task<ActionResult> CreateSpecialDay(
            int branchId, [FromBody] BranchSpecialDayCreateDto dto)
        {
            if (branchId != dto.BranchId)
                return BadRequest(new { Success = false, Message = "BranchId inconsistente." });

            return HandleResult(await _service.CreateSpecialDay(dto));
        }

        /// <summary>
        /// Edita un día especial existente.
        /// La fecha no se puede cambiar — eliminar y recrear si cambia.
        /// </summary>
        [HttpPut("{branchId:int}/special-days/{id:int}")]
        public async Task<ActionResult> UpdateSpecialDay(
            int branchId, int id, [FromBody] BranchSpecialDayUpdateDto dto)
        {
            if (branchId != dto.BranchId || id != dto.Id)
                return BadRequest(new { Success = false, Message = "Id o BranchId inconsistente." });

            return HandleResult(await _service.UpdateSpecialDay(dto));
        }

        /// <summary>Elimina un día especial (eliminación física).</summary>
        [HttpDelete("{branchId:int}/special-days/{id:int}")]
        public async Task<ActionResult> DeleteSpecialDay(int branchId, int id)
            => HandleResult(await _service.DeleteSpecialDay(id));
    }
}