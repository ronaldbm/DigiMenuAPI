using DigiMenuAPI.Application.DTOs.AddDTOs;
using DigiMenuAPI.Application.DTOs.ReadDTOs;
using DigiMenuAPI.Application.DTOs.UpdateDTOs;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace DigiMenuAPI.Controllers
{
    [ApiController]
    [Route("api/subcategories")]
    public class SubcategoryController : Controller
    {
        private readonly IOutputCacheStore outputCacheStore;
        private readonly ISubcategoryService subcategoryService;
        private const string cacheTag = "Products";

        public SubcategoryController(IOutputCacheStore outputCacheStore, ISubcategoryService subcategoryService)
        {
            this.outputCacheStore = outputCacheStore;
            this.subcategoryService = subcategoryService;
        }

        [HttpPost(Name = "CreateSubcategory")]
        [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<int>> Create([FromBody] SubcategoryCreateDto subcategoryDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await subcategoryService.Create(subcategoryDto);

            if (!result.Success)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Error al crear subcategoría",
                    Detail = result.Message,
                    Status = 400
                });
            }

            await outputCacheStore.EvictByTagAsync(cacheTag, default);

            return CreatedAtRoute("GetOneSubcategory", new { id = result.Data }, result.Data);
        }

        [HttpPut(Name = "UpdateSubcategory")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<bool>> Update([FromBody] SubcategoryUpdateDto subcategoryDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await subcategoryService.Update(subcategoryDto);

            if (!result.Success)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Error al actualizar subcategoría",
                    Detail = result.Message,
                    Status = 400
                });
            }

            await outputCacheStore.EvictByTagAsync(cacheTag, default);
            return Ok(true);
        }

        [HttpDelete("{id:int}", Name = "DeleteSubcategory")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<bool>> Delete(int id)
        {
            var result = await subcategoryService.Delete(id);

            if (!result.Success)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Error al eliminar subcategoría",
                    Detail = result.Message,
                    Status = 400
                });
            }

            await outputCacheStore.EvictByTagAsync(cacheTag, default);
            return Ok(true);
        }

        [HttpGet(Name = "GetAllSubcategory")]
        [OutputCache(Tags = [cacheTag])]
        [ProducesResponseType(typeof(List<SubcategoryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<SubcategoryDto>>> GetAll()
        {
            return Ok(await subcategoryService.GetAll());
        }

        [HttpGet("{id:int}", Name = "GetOneSubcategory")]
        [OutputCache(Tags = [cacheTag])]
        [ProducesResponseType(typeof(SubcategoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SubcategoryDto>> GetOne(int id)
        {
            var result = await subcategoryService.GetOne(id);

            return result is null ? NotFound() : Ok(result);
        }
    }
}

