using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    [Route("api/customers")]
    [Authorize]
    public class CustomersController : BaseController
    {
        private readonly ICustomerService _service;

        public CustomersController(ICustomerService service) => _service = service;

        [HttpGet]
        public async Task<ActionResult> GetAll(
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
            => HandleResult(await _service.GetAll(search, page, pageSize));

        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetById(int id)
            => HandleResult(await _service.GetById(id));

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CustomerCreateDto dto)
            => HandleResult(await _service.Create(dto));

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromBody] CustomerUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest(new { Success = false, Message = "El Id de la ruta no coincide con el del body." });

            return HandleResult(await _service.Update(dto));
        }

        [HttpPatch("{id:int}/toggle")]
        public async Task<ActionResult> ToggleActive(int id)
            => HandleResult(await _service.ToggleActive(id));

        [HttpGet("{id:int}/accounts")]
        public async Task<ActionResult> GetCustomerAccounts(
            int id,
            [FromQuery] int? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
            => HandleResult(await _service.GetCustomerAccounts(id, status, page, pageSize));
    }
}
