using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;

namespace DigiMenuAPI.Application.Interfaces
{
    /// <summary>
    /// Gestión de productos activados en una Branch específica (BranchProduct).
    ///
    /// Un BranchProduct es la activación de un producto del catálogo global
    /// en una sucursal, con precio, imagen y visibilidad propios de esa sucursal.
    ///
    /// Reglas de negocio:
    ///   - Un producto solo puede estar activado una vez por Branch (índice único).
    ///   - El BranchId se valida contra la ownership del usuario autenticado.
    ///   - IsDeleted = true desactiva el producto sin borrarlo de la BD.
    ///
    /// Visibilidad de categorías:
    ///   - GetCategoriesWithVisibility: lista categorías que tienen BranchProducts en la Branch.
    ///   - SetCategoryVisibility: activa/desactiva todos los productos de una categoría a la vez.
    /// </summary>
    public interface IBranchProductService
    {
        /// <summary>
        /// Lista todos los BranchProducts activos de una sucursal.
        /// Ordenados por categoría y DisplayOrder.
        /// </summary>
        Task<OperationResult<List<BranchProductReadDto>>> GetByBranch(int branchId);

        /// <summary>
        /// Lista las categorías que tienen BranchProducts en la sucursal,
        /// con el conteo de productos totales y visibles por categoría.
        /// </summary>
        Task<OperationResult<List<BranchCategoryVisibilityDto>>> GetCategoriesWithVisibility(int branchId);

        /// <summary>
        /// Activa un producto del catálogo global en una sucursal.
        /// Falla con Conflict si el producto ya fue activado en esa Branch.
        /// </summary>
        Task<OperationResult<BranchProductReadDto>> Create(BranchProductCreateDto dto);

        /// <summary>
        /// Actualiza precio, categoría, imagen, orden y visibilidad de un BranchProduct.
        /// No permite cambiar el BranchId ni el ProductId.
        /// </summary>
        Task<OperationResult<bool>> Update(BranchProductUpdateDto dto);

        /// <summary>Invierte el flag IsVisible del BranchProduct.</summary>
        Task<OperationResult<bool>> ToggleVisibility(int id);

        /// <summary>
        /// Establece IsVisible en todos los BranchProducts de una categoría para la sucursal.
        /// Permite mostrar u ocultar un bloque completo de productos.
        /// </summary>
        Task<OperationResult<bool>> SetCategoryVisibility(
            int branchId, int categoryId, BranchCategoryVisibilityUpdateDto dto);

        /// <summary>
        /// Desactiva un BranchProduct (soft delete).
        /// Si tiene promociones activas y forceDeletePromotions=false → retorna Conflict.
        /// Si forceDeletePromotions=true → elimina las promos físicamente antes de desactivar.
        /// </summary>
        Task<OperationResult<bool>> Delete(int id, bool forceDeletePromotions = false);

        /// <summary>Reordena varios BranchProducts de una sucursal en una sola operación.</summary>
        Task<OperationResult<bool>> Reorder(int branchId, List<ReorderItemDto> items);
    }
}
