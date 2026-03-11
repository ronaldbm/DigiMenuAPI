using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;

namespace DigiMenuAPI.Application.Interfaces
{
    /// <summary>
    /// Gestión de sucursales del tenant autenticado.
    ///
    /// Reglas de acceso:
    ///   CompanyAdmin → gestiona todas las branches de su empresa.
    ///   BranchAdmin  → solo puede ver su propia branch (sin crear ni eliminar).
    ///   Staff        → sin acceso a gestión de branches.
    ///
    /// Reglas de negocio:
    ///   - No se puede superar Company.MaxBranches al crear (-1 = ilimitado).
    ///   - El Slug es único dentro de la Company.
    ///   - Soft delete: IsDeleted = true, nunca eliminación física.
    ///   - No se puede eliminar una branch con usuarios activos asignados.
    ///   - Al crear una branch se inicializan las 4 entidades de configuración
    ///     (BranchInfo, BranchTheme, BranchLocale, BranchSeo) con valores por defecto.
    /// </summary>
    public interface IBranchService
    {
        /// <summary>
        /// Lista todas las branches activas (no eliminadas) de la empresa autenticada.
        /// </summary>
        Task<OperationResult<List<BranchSummaryDto>>> GetAll();

        /// <summary>
        /// Detalle completo de una branch. Valida que pertenece al tenant autenticado.
        /// </summary>
        Task<OperationResult<BranchReadDto>> GetById(int branchId);

        /// <summary>
        /// Crea una nueva branch con sus 4 entidades de configuración inicializadas.
        /// Valida el límite de branches del plan.
        /// CompanyId se toma del JWT.
        /// </summary>
        Task<OperationResult<BranchReadDto>> Create(BranchCreateDto dto);

        /// <summary>
        /// Edita nombre, slug, dirección, teléfono y email de la branch.
        /// No permite cambiar CompanyId.
        /// </summary>
        Task<OperationResult<BranchReadDto>> Update(BranchUpdateDto dto);

        /// <summary>
        /// Activa o desactiva una branch. Una branch inactiva no aparece
        /// en el menú público pero sus datos se conservan.
        /// </summary>
        Task<OperationResult<bool>> ToggleActive(int branchId);

        /// <summary>
        /// Soft delete. No se puede eliminar si tiene usuarios activos asignados.
        /// Invalida el cache del menú público de la branch.
        /// </summary>
        Task<OperationResult<bool>> Delete(int branchId);
    }
}