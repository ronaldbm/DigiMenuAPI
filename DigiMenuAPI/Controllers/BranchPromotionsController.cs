using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Update;
using System.Collections.Generic;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    /// <summary>
    /// Gestión de promociones para el carrusel de una sucursal (admin).
    ///
    ///   GET    /api/promotions/{branchId}/all   → Lista todas las promociones
    ///   GET    /api/promotions/{id}             → Obtiene una promoción por Id
    ///   POST   /api/promotions                  → Crea una promoción (multipart/form-data)
    ///   PUT    /api/promotions/{id}             → Actualiza una promoción
    ///   DELETE /api/promotions/{id}             → Elimina una promoción
    /// </summary>
    [Route("api/promotions")]
    [Authorize]
    public class BranchPromotionsController : BaseController
    {
        private readonly IBranchPromotionService _service;

        public BranchPromotionsController(IBranchPromotionService service)
        {
            _service = service;
        }

        [HttpGet("{branchId:int}/all")]
        public async Task<ActionResult> GetAll(int branchId)
            => HandleResult(await _service.GetByBranch(branchId));

        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetById(int id)
            => HandleResult(await _service.GetById(id));

        [HttpPost]
        public async Task<ActionResult> Create([FromForm] BranchPromotionCreateDto dto)
            => HandleResult(await _service.Create(dto));

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromForm] BranchPromotionUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest(new { Success = false, Message = "El Id de la ruta no coincide con el del body." });

            return HandleResult(await _service.Update(dto));
        }

        [HttpPatch("{branchId:int}/reorder")]
        public async Task<ActionResult> Reorder(int branchId, [FromBody] List<ReorderItemDto> items)
            => HandleResult(await _service.Reorder(branchId, items));

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
            => HandleResult(await _service.Delete(id));
    }
}
