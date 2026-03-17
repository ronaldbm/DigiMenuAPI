using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    /// <summary>
    /// Gestión de los idiomas habilitados por Company.
    /// Solo accesible a SuperAdmin y CompanyAdmin.
    ///
    ///   GET    /api/company-languages/supported   → Catálogo de plataforma con flag IsSelected
    ///   GET    /api/company-languages             → Idiomas activos de la Company
    ///   POST   /api/company-languages/{code}      → Habilitar un idioma
    ///   DELETE /api/company-languages/{code}      → Deshabilitar un idioma
    ///   PUT    /api/company-languages/{code}/default → Establecer idioma por defecto
    /// </summary>
    [Route("api/company-languages")]
    [Authorize]
    public class CompanyLanguagesController : BaseController
    {
        private readonly ICompanyLanguageService _service;

        public CompanyLanguagesController(ICompanyLanguageService service)
        {
            _service = service;
        }

        [HttpGet("supported")]
        public async Task<ActionResult> GetSupported()
            => HandleResult(await _service.GetSupportedLanguages());

        [HttpGet]
        public async Task<ActionResult> GetCompanyLanguages()
            => HandleResult(await _service.GetCompanyLanguages());

        [HttpPost("{code}")]
        public async Task<ActionResult> Add(string code)
            => HandleResult(await _service.AddLanguage(code));

        [HttpDelete("{code}")]
        public async Task<ActionResult> Remove(string code)
            => HandleResult(await _service.RemoveLanguage(code));

        [HttpPut("{code}/default")]
        public async Task<ActionResult> SetDefault(string code)
            => HandleResult(await _service.SetDefault(code));
    }
}
