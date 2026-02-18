using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Application.DTOs.ReadDTOs;
using DigiMenuAPI.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace DigiMenuAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MenuController : BaseController
    {
        private readonly IStoreService _storeService;
        private const string MenuCacheTag = "tag-menu-publico";

        public MenuController(IStoreService storeService)
        {
            _storeService = storeService;
        }

        [HttpGet(Name = "GetFullMenuStore")]
        [OutputCache(Tags = [MenuCacheTag])]
        public async Task<ActionResult> Get() => HandleResult(await _storeService.GetStoreMenu());

    }
}