using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    /// <summary>
    /// Importación masiva de catálogo: Categorías, Productos y Productos por Sucursal.
    /// Solo CompanyAdmin y SuperAdmin pueden usar estos endpoints.
    /// </summary>
    [Authorize]
    [Route("api/bulk-import")]
    public class BulkImportController : BaseController
    {
        private readonly IBulkImportService _service;

        public BulkImportController(IBulkImportService service)
        {
            _service = service;
        }

        // ── TEMPLATES ────────────────────────────────────────────────

        /// <summary>Devuelve los headers del CSV de categorías para la empresa autenticada.</summary>
        [HttpGet("templates/categories")]
        public async Task<ActionResult> GetCategoryTemplate()
            => HandleResult(await _service.GetCategoryTemplate());

        /// <summary>Devuelve los headers del CSV de productos para la empresa autenticada.</summary>
        [HttpGet("templates/products")]
        public async Task<ActionResult> GetProductTemplate()
            => HandleResult(await _service.GetProductTemplate());

        /// <summary>Devuelve los headers del CSV de productos por sucursal.</summary>
        [HttpGet("templates/branch-products")]
        public async Task<ActionResult> GetBranchProductTemplate()
            => HandleResult(await _service.GetBranchProductTemplate());

        // ── IMPORTS ──────────────────────────────────────────────────

        /// <summary>
        /// Importa categorías desde un payload JSON.
        /// Requiere al menos una traducción en el idioma default de la empresa.
        /// </summary>
        [HttpPost("categories")]
        public async Task<ActionResult> ImportCategories([FromBody] BulkCategoryImportDto dto)
            => HandleResult(await _service.ImportCategories(dto));

        /// <summary>
        /// Importa productos desde FormData.
        /// Acepta JSON del payload serializado + ZIP opcional con imágenes.
        /// </summary>
        [HttpPost("products")]
        [RequestSizeLimit(60 * 1024 * 1024)] // 60 MB max (50 MB ZIP + overhead)
        public async Task<ActionResult> ImportProducts(
            [FromForm] string payload,
            IFormFile? imagesZip)
        {
            BulkProductImportDto? dto;
            try
            {
                dto = System.Text.Json.JsonSerializer.Deserialize<BulkProductImportDto>(
                    payload,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return BadRequest(new { Success = false, Message = "El campo 'payload' no es un JSON válido." });
            }

            if (dto is null)
                return BadRequest(new { Success = false, Message = "El payload es requerido." });

            return HandleResult(await _service.ImportProducts(dto, imagesZip));
        }

        /// <summary>
        /// Importa productos por sucursal desde FormData.
        /// Acepta JSON del payload serializado + ZIP opcional con imágenes de override.
        /// </summary>
        [HttpPost("branch-products")]
        [RequestSizeLimit(60 * 1024 * 1024)]
        public async Task<ActionResult> ImportBranchProducts(
            [FromForm] string payload,
            IFormFile? imagesZip)
        {
            BulkBranchProductImportDto? dto;
            try
            {
                dto = System.Text.Json.JsonSerializer.Deserialize<BulkBranchProductImportDto>(
                    payload,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return BadRequest(new { Success = false, Message = "El campo 'payload' no es un JSON válido." });
            }

            if (dto is null)
                return BadRequest(new { Success = false, Message = "El payload es requerido." });

            return HandleResult(await _service.ImportBranchProducts(dto, imagesZip));
        }
    }
}
