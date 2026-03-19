using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class TagsController : BaseController
    {
        private readonly ITagService _service;

        public TagsController(ITagService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll([FromQuery] string? lang = null)
            => HandleResult(await _service.GetAll(lang));

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id) => HandleResult(await _service.GetById(id));

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] TagCreateDto dto) => HandleResult(await _service.Create(dto));

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, [FromBody] TagUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest("ID inconsistente");

            return HandleResult(await _service.Update(dto));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id) => HandleResult(await _service.Delete(id));
    }
}
