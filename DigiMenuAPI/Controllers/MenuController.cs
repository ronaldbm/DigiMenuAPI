using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace DigiMenuAPI.Controllers
{
    [ApiController]
    [Route("api/menu")]
    public class MenuController : BaseController
    {
        private readonly IStoreService _storeService;

        public MenuController(IStoreService storeService)
        {
            _storeService = storeService;
        }

        /// <summary>
        /// Menú público de una empresa identificada por su slug.
        /// GET /api/menu/mi-restaurante
        /// No requiere JWT.
        /// </summary>
        [HttpGet("{slug}")]
        [AllowAnonymous]
        [OutputCache(Tags = ["tag-menu-publico"])]
        public async Task<ActionResult> Get(string slug) => HandleResult(await _storeService.GetStoreMenu(slug));
    }
}