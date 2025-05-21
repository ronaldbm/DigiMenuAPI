using DigiMenuAPI.Application.DTOs.ReadDTOs;
using DigiMenuAPI.Application.DTOs.UpdateDTOs;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    [ApiController]
    [Route("api/socialLinks")]
    public class SocialLinkController : Controller
    {
        private readonly ISocialLinkService socialLinkService;
        public SocialLinkController(ISocialLinkService socialLinkService)
        {
            this.socialLinkService = socialLinkService;
        }


        [HttpPut(Name = "UpdateSocialLinks")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<bool>> Update([FromBody] List<SocialLinkUpdateDto> socialLinkDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await socialLinkService.Update(socialLinkDto);

            if (!result.Success)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Error al actualizar los links de redes sociales",
                    Detail = result.Message,
                    Status = 400
                });
            }

            return Ok(true);
        }

        [HttpGet(Name = "GetAllSocialLink")]
        [ProducesResponseType(typeof(List<SocialLinkDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<SocialLinkDto>>> GetAll()
        {
            return Ok(await socialLinkService.GetAll());
        }

    }
}
