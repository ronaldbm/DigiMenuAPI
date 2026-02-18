using DigiMenuAPI.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        protected ActionResult HandleResult<T>(OperationResult<T> result)
        {
            if (result is null) return NotFound();

            if (result.Success && result.Data is not null)
                return Ok(result);

            if (result.Success && result.Data is null)
                return NoContent();

            // Si falló, enviamos un BadRequest con el mensaje de error estructurado
            return BadRequest(result);
        }
    }
}