using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    [Route("api/[controller]")]
    public class ProductsController : BaseController
    {
        private readonly IProductService _service;

        public ProductsController(IProductService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
            => HandleResult(await _service.GetAll(page, pageSize));

        /// <summary>
        /// Lista compacta de todos los productos sin paginación.
        /// Útil para modales de selección al crear BranchProducts.
        /// </summary>
        [HttpGet("all")]
        public async Task<ActionResult> GetAllSimple()
            => HandleResult(await _service.GetAllSimple());

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id) => HandleResult(await _service.GetById(id));

        [HttpGet("admin/{id}")]
        public async Task<ActionResult> GetForEdit(int id) => HandleResult(await _service.GetForEdit(id));

        [HttpPost]
        public async Task<ActionResult> Create([FromForm] ProductCreateDto dto) => HandleResult(await _service.Create(dto));

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, [FromForm] ProductUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest("ID inconsistente");

            return HandleResult(await _service.Update(dto));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id) => HandleResult(await _service.Delete(id));

        // ── Traducciones ──────────────────────────────────────────────

        /// <summary>Crea o actualiza la traducción de un producto para el idioma {code}.</summary>
        [HttpPut("{id}/translations/{code}")]
        [Authorize]
        public async Task<ActionResult> UpsertTranslation(
            int id, string code, [FromBody] ProductTranslationUpsertDto dto)
            => HandleResult(await _service.UpsertTranslation(id, code, dto));

        /// <summary>Elimina la traducción de un producto para el idioma {code}.</summary>
        [HttpDelete("{id}/translations/{code}")]
        [Authorize]
        public async Task<ActionResult> DeleteTranslation(int id, string code)
            => HandleResult(await _service.DeleteTranslation(id, code));
    }
}