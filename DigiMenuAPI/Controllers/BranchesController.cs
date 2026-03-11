using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    /// <summary>
    /// Gestión de sucursales del tenant autenticado.
    ///
    /// GET    /api/branches          → Listar sucursales de la empresa
    /// GET    /api/branches/{id}     → Detalle de una sucursal
    /// POST   /api/branches          → Crear nueva sucursal
    /// PUT    /api/branches/{id}     → Editar sucursal
    /// PATCH  /api/branches/{id}/toggle-active → Activar / desactivar
    /// DELETE /api/branches/{id}     → Soft delete
    /// </summary>
    [Route("api/branches")]
    [Authorize]
    public class BranchesController : BaseController
    {
        private readonly IBranchService _service;

        public BranchesController(IBranchService service)
        {
            _service = service;
        }

        /// <summary>Lista todas las sucursales activas de la empresa autenticada.</summary>
        [HttpGet]
        public async Task<ActionResult> GetAll()
            => HandleResult(await _service.GetAll());

        /// <summary>Detalle completo de una sucursal.</summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetById(int id)
            => HandleResult(await _service.GetById(id));

        /// <summary>
        /// Crea una nueva sucursal con configuración inicial por defecto.
        /// Requiere rol CompanyAdmin o superior.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] BranchCreateDto dto)
            => HandleResult(await _service.Create(dto));

        /// <summary>
        /// Edita los datos básicos de una sucursal.
        /// Requiere rol CompanyAdmin o superior.
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromBody] BranchUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest("El Id de la ruta no coincide con el del body.");

            return HandleResult(await _service.Update(dto));
        }

        /// <summary>
        /// Activa o desactiva una sucursal.
        /// Una sucursal inactiva no aparece en el menú público.
        /// </summary>
        [HttpPatch("{id:int}/toggle-active")]
        public async Task<ActionResult> ToggleActive(int id)
            => HandleResult(await _service.ToggleActive(id));

        /// <summary>
        /// Elimina una sucursal (soft delete).
        /// No permitido si tiene usuarios activos asignados.
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
            => HandleResult(await _service.Delete(id));
    }
}