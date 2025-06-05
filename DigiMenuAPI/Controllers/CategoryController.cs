using DigiMenuAPI.Application.DTOs.AddDTOs;
using DigiMenuAPI.Application.DTOs.ReadDTOs;
using DigiMenuAPI.Application.DTOs.UpdateDTOs;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace DigiMenuAPI.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoryController : Controller
    {
        private readonly IOutputCacheStore outputCacheStore;
        private readonly ICategoryService categoryService;
        private const string cacheTag = "Products";

        public CategoryController(IOutputCacheStore outputCacheStore, ICategoryService categoryService)
        {
            this.outputCacheStore = outputCacheStore;
            this.categoryService = categoryService;
        }

        [HttpPost(Name = "CreateCategory")]
        [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<int>> Create([FromBody] CategoryCreateDto categoryDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await categoryService.Create(categoryDto);

            if (!result.Success)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Error al crear categoría",
                    Detail = result.Message,
                    Status = 400
                });
            }

            await outputCacheStore.EvictByTagAsync(cacheTag, default);

            return CreatedAtRoute("GetOneCategory", new { id = result.Data }, result.Data);
        }

        [HttpPut(Name = "UpdateCategory")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<bool>> Update([FromBody] CategoryUpdateDto categoryDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await categoryService.Update(categoryDto);

            if (!result.Success)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Error al actualizar categoría",
                    Detail = result.Message,
                    Status = 400
                });
            }

            await outputCacheStore.EvictByTagAsync(cacheTag, default);
            return Ok(true);
        }

        [HttpDelete("{id:int}", Name = "DeleteCategory")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<bool>> Delete(int id)
        {
            var result = await categoryService.Delete(id);

            if (!result.Success)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Error al eliminar categoría",
                    Detail = result.Message,
                    Status = 400
                });
            }

            await outputCacheStore.EvictByTagAsync(cacheTag, default);
            return Ok(true);
        }

        [HttpGet(Name = "GetAllCategory")]
        [OutputCache(Tags = [cacheTag])]
        [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CategoryDto>>> GetAll()
        {
            return Ok(await categoryService.GetAll());
        }

        [HttpGet("{id:int}", Name = "GetOneCategory")]
        [OutputCache(Tags = [cacheTag])]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CategoryDto>> GetOne(int id)
        {
            var result = await categoryService.GetOne(id);

            return result is null ? NotFound() : Ok(result);
        }        
        
        [HttpGet("GetBasicInformation", Name = "GetBasicInformation")]
        [OutputCache(Tags = [cacheTag])]
        [ProducesResponseType(typeof(CategoryInfoDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CategoryInfoDto>>> GetBasicInformation()
        {
            return await categoryService.GetBasicInformation();
        }        
        
        [HttpGet("GetCategorySelectInformation", Name = "GetCategorySelectInformation")]
        [OutputCache(Tags = [cacheTag])]
        [ProducesResponseType(typeof(CategorySelectInformation), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CategorySelectInformation>>> GetCategorySelectInformation()
        {
            return await categoryService.GetCategorySelectInformation();
        }
    }
}

