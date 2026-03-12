using AppCore.Application.Common;
using AppCore.Filters;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    /// <summary>
    /// Gestión de configuración de una Branch, separada en 5 secciones.
    ///
    /// Todas las rutas requieren autenticación JWT.
    /// La validación de ownership de Branch se realiza en el servicio.
    ///
    /// GET  /api/settings/{branchId}                → Todo (compuesto)
    /// GET  /api/settings/{branchId}/info           → Identidad
    /// GET  /api/settings/{branchId}/theme          → Tema visual
    /// GET  /api/settings/{branchId}/locale         → Localización
    /// GET  /api/settings/{branchId}/seo            → SEO y analytics
    /// GET  /api/settings/{branchId}/reservation-form → Formulario (módulo RESERVATIONS)
    /// PATCH /api/settings/{branchId}/info          → Actualiza identidad
    /// PATCH /api/settings/{branchId}/theme         → Actualiza tema
    /// PATCH /api/settings/{branchId}/locale        → Actualiza localización
    /// PATCH /api/settings/{branchId}/seo           → Actualiza SEO
    /// PATCH /api/settings/{branchId}/reservation-form → Actualiza formulario
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

        // ── GET ───────────────────────────────────────────────────────

        /// <summary>Configuración completa de la Branch (5 secciones en un objeto).</summary>
        [HttpGet("{branchId:int}")]
        public async Task<ActionResult> GetAll(int branchId)
            => HandleResult(await _service.GetAll(branchId));

        /// <summary>Solo identidad del negocio.</summary>
        [HttpGet("{branchId:int}/info")]
        public async Task<ActionResult> GetInfo(int branchId)
            => HandleResult(await _service.GetInfo(branchId));

        /// <summary>Solo tema visual y layout.</summary>
        [HttpGet("{branchId:int}/theme")]
        public async Task<ActionResult> GetTheme(int branchId)
            => HandleResult(await _service.GetTheme(branchId));

        /// <summary>Solo configuración regional.</summary>
        [HttpGet("{branchId:int}/locale")]
        public async Task<ActionResult> GetLocale(int branchId)
            => HandleResult(await _service.GetLocale(branchId));

        /// <summary>Solo SEO y analytics.</summary>
        [HttpGet("{branchId:int}/seo")]
        public async Task<ActionResult> GetSeo(int branchId)
            => HandleResult(await _service.GetSeo(branchId));

        /// <summary>Formulario de reservas. Requiere módulo RESERVATIONS activo.</summary>
        [HttpGet("{branchId:int}/reservation-form")]
        [RequireModule(ModuleCodes.Reservations)]
        public async Task<ActionResult> GetReservationForm(int branchId)
            => HandleResult(await _service.GetReservationForm(branchId));

        // ── PATCH ─────────────────────────────────────────────────────

        /// <summary>Actualiza identidad del negocio (incluye subida de imágenes).</summary>
        [HttpPatch("{branchId:int}/info")]
        public async Task<ActionResult> UpdateInfo(
            int branchId, [FromForm] BranchInfoUpdateDto dto)
        {
            // Garantiza que el branchId de la ruta coincide con el del DTO
            if (branchId != dto.BranchId)
                return BadRequest(new { Success = false, Message = "BranchId inconsistente." });

            return HandleResult(await _service.UpdateInfo(dto));
        }

        /// <summary>Actualiza tema visual y layout.</summary>
        [HttpPatch("{branchId:int}/theme")]
        public async Task<ActionResult> UpdateTheme(
            int branchId, [FromBody] BranchThemeUpdateDto dto)
        {
            if (branchId != dto.BranchId)
                return BadRequest(new { Success = false, Message = "BranchId inconsistente." });

            return HandleResult(await _service.UpdateTheme(dto));
        }

        /// <summary>Actualiza configuración regional.</summary>
        [HttpPatch("{branchId:int}/locale")]
        public async Task<ActionResult> UpdateLocale(
            int branchId, [FromBody] BranchLocaleUpdateDto dto)
        {
            if (branchId != dto.BranchId)
                return BadRequest(new { Success = false, Message = "BranchId inconsistente." });

            return HandleResult(await _service.UpdateLocale(dto));
        }

        /// <summary>Actualiza SEO y analytics.</summary>
        [HttpPatch("{branchId:int}/seo")]
        public async Task<ActionResult> UpdateSeo(
            int branchId, [FromBody] BranchSeoUpdateDto dto)
        {
            if (branchId != dto.BranchId)
                return BadRequest(new { Success = false, Message = "BranchId inconsistente." });

            return HandleResult(await _service.UpdateSeo(dto));
        }

        /// <summary>Actualiza formulario de reservas. Requiere módulo RESERVATIONS activo.</summary>
        [HttpPatch("{branchId:int}/reservation-form")]
        [RequireModule(ModuleCodes.Reservations)]
        public async Task<ActionResult> UpdateReservationForm(
            int branchId, [FromBody] BranchReservationFormUpdateDto dto)
        {
            if (branchId != dto.BranchId)
                return BadRequest(new { Success = false, Message = "BranchId inconsistente." });

            return HandleResult(await _service.UpdateReservationForm(dto));
        }
    }
}