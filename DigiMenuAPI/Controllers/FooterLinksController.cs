using DigiMenuAPI.Application.DTOs.Add;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    [Route("api/[controller]")]
    public class FooterLinksController : BaseController
    {
        private readonly IFooterLinkService _service;

        public FooterLinksController(IFooterLinkService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll() => HandleResult(await _service.GetAll());

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] FooterLinkCreateDto dto) => HandleResult(await _service.Create(dto));

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, [FromBody] FooterLinkUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest("ID inconsistente");

            return HandleResult(await _service.Update(dto));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id) => HandleResult(await _service.Delete(id));
    }
}