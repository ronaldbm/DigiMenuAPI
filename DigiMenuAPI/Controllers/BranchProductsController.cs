using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    /// <summary>
    /// Gestión del menú propio de cada sucursal (BranchProducts).
    ///
    /// Un BranchProduct es la activación de un producto del catálogo global
    /// en una sucursal específica, con precio, imagen y visibilidad propios.
    ///
    /// Productos por sucursal:
    ///   GET    /api/branch-products/{branchId}                       → Listar productos activados
    ///   POST   /api/branch-products                                  → Activar producto en sucursal
    ///   PUT    /api/branch-products/{id}                             → Editar configuración
    ///   PATCH  /api/branch-products/{id}/visibility                  → Alternar visibilidad
    ///   DELETE /api/branch-products/{id}                             → Desactivar (soft delete)
    ///
    /// Visibilidad de categorías:
    ///   GET   /api/branch-products/{branchId}/categories             → Estado de visibilidad por categoría
    ///   PATCH /api/branch-products/{branchId}/categories/{catId}/visibility → Mostrar/ocultar categoría completa
    /// </summary>
    [Route("api/branch-products")]
    [Authorize]
    public class BranchProductsController : BaseController
    {
        private readonly IBranchProductService _service;

        public BranchProductsController(IBranchProductService service)
        {
            _service = service;
        }

        /// <summary>Lista todos los BranchProducts activos de una sucursal, ordenados por categoría y DisplayOrder.</summary>
        [HttpGet("{branchId:int}")]
        public async Task<ActionResult> GetByBranch(int branchId)
            => HandleResult(await _service.GetByBranch(branchId));

        /// <summary>
        /// Lista las categorías con BranchProducts en la sucursal y su estado de visibilidad.
        /// Incluye conteo de productos totales y visibles por categoría.
        /// </summary>
        [HttpGet("{branchId:int}/categories")]
        public async Task<ActionResult> GetCategoriesWithVisibility(int branchId)
            => HandleResult(await _service.GetCategoriesWithVisibility(branchId));

        /// <summary>
        /// Activa un producto del catálogo global en una sucursal.
        /// Falla con 409 si el producto ya estaba activado en esa Branch.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Create([FromForm] BranchProductCreateDto dto)
            => HandleResult(await _service.Create(dto));

        /// <summary>
        /// Edita precio, categoría, imagen, orden y visibilidad de un BranchProduct.
        /// No se puede cambiar el producto base ni la sucursal.
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromForm] BranchProductUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest(new { Success = false, Message = "El Id de la ruta no coincide con el del body." });

            return HandleResult(await _service.Update(dto));
        }

        /// <summary>Alterna la visibilidad (IsVisible) de un BranchProduct individual.</summary>
        [HttpPatch("{id:int}/visibility")]
        public async Task<ActionResult> ToggleVisibility(int id)
            => HandleResult(await _service.ToggleVisibility(id));

        /// <summary>
        /// Establece la visibilidad de todos los productos de una categoría en la sucursal.
        /// Útil para mostrar u ocultar un bloque completo de la carta.
        /// </summary>
        [HttpPatch("{branchId:int}/categories/{categoryId:int}/visibility")]
        public async Task<ActionResult> SetCategoryVisibility(
            int branchId,
            int categoryId,
            [FromBody] BranchCategoryVisibilityUpdateDto dto)
            => HandleResult(await _service.SetCategoryVisibility(branchId, categoryId, dto));

        /// <summary>Desactiva un BranchProduct (soft delete). No afecta el catálogo global.</summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
            => HandleResult(await _service.Delete(id));
    }
}
