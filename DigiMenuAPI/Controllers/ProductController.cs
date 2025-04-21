using DigiMenuAPI.Application.DTOs.AddDTOs;
using DigiMenuAPI.Application.DTOs.ReadDTOs;
using DigiMenuAPI.Application.DTOs.UpdateDTOs;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace DigiMenuAPI.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductController : Controller
    {
        private readonly IOutputCacheStore outputCacheStore;
        private readonly IProductService productService;
        private const string cacheTag = "Product";

        public ProductController(IOutputCacheStore outputCacheStore, IProductService productService)
        {
            this.outputCacheStore = outputCacheStore;
            this.productService = productService;
        }

        [HttpPost(Name = "Create")]
        [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<int>> Create([FromBody] ProductCreateDto productDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await productService.Create(productDto);

            if (!result.Success)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Error al crear producto",
                    Detail = result.Message,
                    Status = 400
                });
            }

            await outputCacheStore.EvictByTagAsync(cacheTag, default);

            return CreatedAtRoute("GetOne", new { id = result.Data }, result.Data);
        }

        [HttpPut(Name = "Update")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<bool>> Update([FromBody] ProductUpdateDto productDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await productService.Update(productDto);

            if (!result.Success)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Error al actualizar producto",
                    Detail = result.Message,
                    Status = 400
                });
            }

            await outputCacheStore.EvictByTagAsync(cacheTag, default);
            return Ok(true);
        }

        [HttpDelete("{id:int}", Name = "Delete")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<bool>> Delete(int id)
        {
            var result = await productService.Delete(id);

            if (!result.Success)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Error al eliminar producto",
                    Detail = result.Message,
                    Status = 400
                });
            }

            await outputCacheStore.EvictByTagAsync(cacheTag, default);
            return Ok(true);
        }

        [HttpGet(Name = "GetAll")]
        [OutputCache(Tags = [cacheTag])]
        [ProducesResponseType(typeof(List<ProductDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ProductDto>>> GetAll()
        {
            return Ok(await productService.GetAll());
        }

        [HttpGet("{id:int}", Name = "GetOne")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductDto>> GetOne(int id)
        {
            var result = await productService.GetOne(id);

            return result is null ? NotFound() : Ok(result);
        }
    }
}

