using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class CategoriesController : BaseController
    {
        private readonly ICategoryService _service;

        public CategoriesController(ICategoryService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll([FromQuery] string? lang = null)
            => HandleResult(await _service.GetAll(lang));

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id) => HandleResult(await _service.GetById(id));

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CategoryCreateDto dto) => HandleResult(await _service.Create(dto));

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, [FromBody] CategoryUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest("El ID no coincide");

            return HandleResult(await _service.Update(dto));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id) => HandleResult(await _service.Delete(id));

        /// <summary>Reordena varias categorías en una sola llamada.</summary>
        [HttpPatch("reorder")]
        public async Task<ActionResult> Reorder([FromBody] List<ReorderItemDto> items)
            => HandleResult(await _service.Reorder(items));
    }
}
