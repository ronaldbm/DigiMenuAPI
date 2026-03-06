using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class FooterLinksController : BaseController
    {
        private readonly IFooterLinkService _service;

        public FooterLinksController(IFooterLinkService service)
        {
            _service = service;
        }

        /// <summary>
        /// GET api/footerlinks?branchId=3
        /// CompanyAdmin pasa el branchId que quiere consultar.
        /// BranchAdmin/Staff solo pueden consultar su propia Branch (validado en servicio).
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> GetAll([FromQuery] int branchId)
            => HandleResult(await _service.GetAll(branchId));

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] FooterLinkCreateDto dto)
            => HandleResult(await _service.Create(dto));

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromBody] FooterLinkUpdateDto dto)
        {
            if (id != dto.Id) return BadRequest("ID inconsistente.");
            return HandleResult(await _service.Update(dto));
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
            => HandleResult(await _service.Delete(id));
    }
}