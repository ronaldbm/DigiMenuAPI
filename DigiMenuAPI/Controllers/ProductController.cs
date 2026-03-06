using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
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
        public async Task<ActionResult> GetAll() => HandleResult(await _service.GetAll());

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
    }
}