using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    [Route("api/[controller]")]
    public class StandardIconsController : BaseController
    {
        private readonly IStandardIconService _service;

        public StandardIconsController(IStandardIconService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll() => HandleResult(await _service.GetAll());
    }
}