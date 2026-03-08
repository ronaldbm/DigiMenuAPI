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
        /// Menú público de una Branch identificada por companySlug + branchSlug.
        /// GET /api/menu/el-rancho/centro
        ///
        /// El cache se aplica por combinación de slugs (= por Branch específica).
        /// Un cambio en la empresa A nunca invalida el cache de la empresa B.
        ///
        /// Invalidación desde servicios via ICacheService:
        ///   menu-branch:{branchId}   → Setting, FooterLinks, BranchProducts.
        ///   menu-company:{companyId} → Category, Product, Tag (catálogo global).
        ///
        /// No requiere JWT — endpoint público.
        /// </summary>
        [HttpGet("{companySlug}/{branchSlug}")]
        [AllowAnonymous]
        [OutputCache(VaryByRouteValueNames = ["companySlug", "branchSlug"])]
        public async Task<ActionResult> Get(string companySlug, string branchSlug)
            => HandleResult(await _storeService.GetStoreMenu(companySlug, branchSlug));
    }
}