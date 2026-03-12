using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;

namespace DigiMenuAPI.Application.Interfaces
{
    /// <summary>
    /// Gestión de la configuración de una Branch, separada en 5 secciones independientes.
    ///
    /// Cada sección tiene su propio ciclo de vida y puede actualizarse
    /// sin afectar las demás. El GET general agrega todo para el panel admin.
    ///
    /// Seguridad: todos los métodos validan que la Branch pertenece al tenant
    /// autenticado via ITenantService.ValidateBranchOwnershipAsync.
    /// </summary>
    public interface ISettingService
    {
        // ── LECTURA ───────────────────────────────────────────────────

        /// <summary>
        /// Devuelve las 5 secciones de configuración en un objeto compuesto.
        /// ReservationForm es null si el módulo RESERVATIONS no está activo.
        /// Usar al cargar la página de configuración del panel admin.
        /// </summary>
        Task<OperationResult<BranchSettingsReadDto>> GetAll(int branchId);

        Task<OperationResult<BranchInfoReadDto>> GetInfo(int branchId);
        Task<OperationResult<BranchThemeReadDto>> GetTheme(int branchId);
        Task<OperationResult<BranchLocaleReadDto>> GetLocale(int branchId);
        Task<OperationResult<BranchSeoReadDto>> GetSeo(int branchId);

        /// <summary>Requiere módulo RESERVATIONS activo.</summary>
        Task<OperationResult<BranchReservationFormReadDto>> GetReservationForm(int branchId);

        // ── ACTUALIZACIÓN ─────────────────────────────────────────────

        Task<OperationResult<BranchInfoReadDto>> UpdateInfo(BranchInfoUpdateDto dto);
        Task<OperationResult<BranchThemeReadDto>> UpdateTheme(BranchThemeUpdateDto dto);
        Task<OperationResult<BranchLocaleReadDto>> UpdateLocale(BranchLocaleUpdateDto dto);
        Task<OperationResult<BranchSeoReadDto>> UpdateSeo(BranchSeoUpdateDto dto);

        /// <summary>Requiere módulo RESERVATIONS activo.</summary>
        Task<OperationResult<BranchReservationFormReadDto>> UpdateReservationForm(
            BranchReservationFormUpdateDto dto);
    }
}