using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface IProductService
    {
        /// <summary>
        /// Listado paginado del catálogo de la empresa autenticada.
        /// Si se pasa lang, resuelve el nombre en ese idioma (con fallback a la primera traducción disponible).
        /// </summary>
        Task<OperationResult<PagedResult<ProductListItemDto>>> GetAll(int page = 1, int pageSize = 20, string? lang = null);

        /// <summary>
        /// Lista compacta de todos los productos sin paginación.
        /// Usada en modales de selección para crear BranchProducts.
        /// </summary>
        Task<OperationResult<List<ProductSummaryDto>>> GetAllSimple();

        Task<OperationResult<ProductReadDto>> GetById(int id);

        /// <summary>Vista completa con traducciones para el formulario de edición.</summary>
        Task<OperationResult<ProductAdminReadDto>> GetForEdit(int id);

        /// <summary>CompanyId se inyecta desde el JWT — no viene en el DTO.</summary>
        Task<OperationResult<ProductReadDto>> Create(ProductCreateDto dto);
        Task<OperationResult<bool>> Update(ProductUpdateDto dto);
        Task<OperationResult<bool>> Delete(int id);

        /// <summary>
        /// Devuelve los nombres de las etiquetas de un producto resueltos al idioma solicitado.
        /// Llamado bajo demanda desde el frontend (tooltip lazy).
        /// </summary>
        Task<OperationResult<List<TagTooltipDto>>> GetTagNames(int productId, string? lang);
    }
}
