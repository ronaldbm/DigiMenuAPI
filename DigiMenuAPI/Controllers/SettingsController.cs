using AppCore.Application.Common;
using AppCore.Filters;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    /// <summary>
    /// Gestión de configuración en dos niveles: Company y Branch.
    ///
    /// Todas las rutas requieren autenticación JWT.
    /// La validación de ownership se realiza en el servicio.
    ///
    /// Company-level (companyId viene del JWT):
    /// GET  /api/settings/company              → Todo (Info + Theme + Seo)
    /// GET  /api/settings/company/info         → Identidad
    /// GET  /api/settings/company/theme        → Tema visual
    /// GET  /api/settings/company/seo          → SEO y analytics
    /// PATCH /api/settings/company/info        → Actualiza identidad
    /// PATCH /api/settings/company/theme       → Actualiza tema
    /// PATCH /api/settings/company/seo         → Actualiza SEO
    ///
    /// Branch-level:
    /// GET  /api/settings/branch/{branchId}              → Todo (Locale + ReservationForm)
    /// GET  /api/settings/branch/{branchId}/locale       → Localización
    /// GET  /api/settings/branch/{branchId}/reservation-form → Formulario (módulo RESERVATIONS)
    /// PATCH /api/settings/branch/{branchId}/locale      → Actualiza localización
    /// PATCH /api/settings/branch/{branchId}/reservation-form → Actualiza formulario
    /// </summary>
    [Route("api/settings")]
    [Authorize]
    public class SettingsController : BaseController
    {
        private readonly ISettingService _service;

        public SettingsController(ISettingService service)
        {
            _service = service;
        }

        // ── Company-level: GET ────────────────────────────────────────

        /// <summary>Configuración completa de la Company (Info + Theme + Seo).</summary>
        [HttpGet("company")]
        public async Task<ActionResult> GetCompanySettings()
            => HandleResult(await _service.GetCompanySettings());

        /// <summary>Solo identidad del negocio.</summary>
        [HttpGet("company/info")]
        public async Task<ActionResult> GetCompanyInfo()
            => HandleResult(await _service.GetCompanyInfo());

        /// <summary>Solo tema visual y layout.</summary>
        [HttpGet("company/theme")]
        public async Task<ActionResult> GetCompanyTheme()
            => HandleResult(await _service.GetCompanyTheme());

        /// <summary>Solo SEO y analytics.</summary>
        [HttpGet("company/seo")]
        public async Task<ActionResult> GetCompanySeo()
            => HandleResult(await _service.GetCompanySeo());

        // ── Company-level: PATCH ──────────────────────────────────────

        /// <summary>Actualiza identidad del negocio (incluye subida de imágenes).</summary>
        [HttpPatch("company/info")]
        public async Task<ActionResult> UpdateCompanyInfo([FromForm] CompanyInfoUpdateDto dto)
            => HandleResult(await _service.UpdateCompanyInfo(dto));

        /// <summary>Actualiza tema visual y layout.</summary>
        [HttpPatch("company/theme")]
        public async Task<ActionResult> UpdateCompanyTheme([FromBody] CompanyThemeUpdateDto dto)
            => HandleResult(await _service.UpdateCompanyTheme(dto));

        /// <summary>Actualiza SEO y analytics.</summary>
        [HttpPatch("company/seo")]
        public async Task<ActionResult> UpdateCompanySeo([FromBody] CompanySeoUpdateDto dto)
            => HandleResult(await _service.UpdateCompanySeo(dto));

        // ── Branch-level: GET ─────────────────────────────────────────

        /// <summary>Configuración completa de la Branch (Locale + ReservationForm).</summary>
        [HttpGet("branch/{branchId:int}")]
        public async Task<ActionResult> GetBranchSettings(int branchId)
            => HandleResult(await _service.GetBranchSettings(branchId));

        /// <summary>Solo configuración regional.</summary>
        [HttpGet("branch/{branchId:int}/locale")]
        public async Task<ActionResult> GetBranchLocale(int branchId)
            => HandleResult(await _service.GetBranchLocale(branchId));

        /// <summary>Formulario de reservas. Requiere módulo RESERVATIONS activo.</summary>
        [HttpGet("branch/{branchId:int}/reservation-form")]
        [RequireModule(ModuleCodes.Reservations)]
        public async Task<ActionResult> GetReservationForm(int branchId)
            => HandleResult(await _service.GetReservationForm(branchId));

        // ── Branch-level: PATCH ───────────────────────────────────────

        /// <summary>Actualiza configuración regional.</summary>
        [HttpPatch("branch/{branchId:int}/locale")]
        public async Task<ActionResult> UpdateBranchLocale(
            int branchId, [FromBody] BranchLocaleUpdateDto dto)
        {
            if (branchId != dto.BranchId)
                return BadRequest(new { Success = false, Message = "BranchId inconsistente." });

            return HandleResult(await _service.UpdateBranchLocale(dto));
        }

        /// <summary>Actualiza formulario de reservas. Requiere módulo RESERVATIONS activo.</summary>
        [HttpPatch("branch/{branchId:int}/reservation-form")]
        [RequireModule(ModuleCodes.Reservations)]
        public async Task<ActionResult> UpdateReservationForm(
            int branchId, [FromBody] BranchReservationFormUpdateDto dto)
        {
            if (branchId != dto.BranchId)
                return BadRequest(new { Success = false, Message = "BranchId inconsistente." });

            return HandleResult(await _service.UpdateReservationForm(dto));
        }

        // ── Contact: GET / PATCH ──────────────────────────────────────

        /// <summary>Datos de contacto de la empresa. Solo CompanyAdmin o superior.</summary>
        [HttpGet("company/contact")]
        public async Task<ActionResult> GetCompanyContact()
            => HandleResult(await _service.GetCompanyContact());

        /// <summary>Actualiza datos de contacto de la empresa. Solo CompanyAdmin o superior.</summary>
        [HttpPatch("company/contact")]
        public async Task<ActionResult> UpdateCompanyContact([FromBody] CompanyContactUpdateDto dto)
            => HandleResult(await _service.UpdateCompanyContact(dto));

        /// <summary>Datos de contacto de una sucursal. Staff no permitido.</summary>
        [HttpGet("branch/{branchId:int}/contact")]
        public async Task<ActionResult> GetBranchContact(int branchId)
            => HandleResult(await _service.GetBranchContact(branchId));

        /// <summary>Actualiza datos de contacto de una sucursal. Staff no permitido.</summary>
        [HttpPatch("branch/{branchId:int}/contact")]
        public async Task<ActionResult> UpdateBranchContact(
            int branchId, [FromBody] BranchContactUpdateDto dto)
            => HandleResult(await _service.UpdateBranchContact(branchId, dto));

        // ── Tabs ──────────────────────────────────────────────────────────

        /// <summary>Actualiza las pestañas visibles de la Company.</summary>
        [HttpPatch("company/tabs")]
        public async Task<ActionResult> UpdateCompanyTabs([FromBody] CompanyTabsUpdateDto dto)
            => HandleResult(await _service.UpdateCompanyTabs(dto));

    }
}
