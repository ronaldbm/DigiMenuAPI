using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    [Authorize]
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
            [FromQuery] int pageSize = 20,
            [FromQuery] string? lang = null)
            => HandleResult(await _service.GetAll(page, pageSize, lang));

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

        /// <summary>
        /// Devuelve los nombres de las etiquetas de un producto resueltos al idioma solicitado.
        /// Endpoint ligero usado por el tooltip de la lista de productos.
        /// </summary>
        [HttpGet("{id}/tags")]
        public async Task<ActionResult> GetTagNames(int id, [FromQuery] string? lang = null)
            => HandleResult(await _service.GetTagNames(id, lang));

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id) => HandleResult(await _service.Delete(id));
    }
}
