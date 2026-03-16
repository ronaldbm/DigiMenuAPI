using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;

namespace DigiMenuAPI.Application.Interfaces
{
    /// <summary>
    /// Gestión de la configuración, separada en dos niveles:
    ///
    /// Company-level: Info (identidad), Theme (visual), Seo.
    ///   CompanyId se extrae del JWT via ITenantService.GetCompanyId().
    ///
    /// Branch-level: Locale, ReservationForm.
    ///   BranchId se pasa como parámetro; ownership se valida en el servicio.
    ///
    /// Cada sección tiene su propio ciclo de vida y puede actualizarse
    /// sin afectar las demás. Los GET generales agregan todo para el panel admin.
    /// </summary>
    public interface ISettingService
    {
        // ── Company-level: LECTURA ────────────────────────────────────

        /// <summary>
        /// Devuelve las 3 secciones de configuración de la Company en un objeto compuesto.
        /// CompanyId se obtiene del JWT.
        /// </summary>
        Task<OperationResult<CompanySettingsReadDto>> GetCompanySettings();

        Task<OperationResult<CompanyInfoReadDto>> GetCompanyInfo();
        Task<OperationResult<CompanyThemeReadDto>> GetCompanyTheme();
        Task<OperationResult<CompanySeoReadDto>> GetCompanySeo();

        // ── Company-level: ACTUALIZACIÓN ─────────────────────────────

        Task<OperationResult<CompanyInfoReadDto>> UpdateCompanyInfo(CompanyInfoUpdateDto dto);
        Task<OperationResult<CompanyThemeReadDto>> UpdateCompanyTheme(CompanyThemeUpdateDto dto);
        Task<OperationResult<CompanySeoReadDto>> UpdateCompanySeo(CompanySeoUpdateDto dto);

        // ── Branch-level: LECTURA ─────────────────────────────────────

        /// <summary>
        /// Devuelve Locale y ReservationForm (si módulo activo) de una Branch.
        /// ReservationForm es null si el módulo RESERVATIONS no está activo.
        /// </summary>
        Task<OperationResult<BranchSettingsReadDto>> GetBranchSettings(int branchId);

        Task<OperationResult<BranchLocaleReadDto>> GetBranchLocale(int branchId);

        /// <summary>Requiere módulo RESERVATIONS activo.</summary>
        Task<OperationResult<BranchReservationFormReadDto>> GetReservationForm(int branchId);

        // ── Branch-level: ACTUALIZACIÓN ───────────────────────────────

        Task<OperationResult<BranchLocaleReadDto>> UpdateBranchLocale(BranchLocaleUpdateDto dto);

        /// <summary>Requiere módulo RESERVATIONS activo.</summary>
        Task<OperationResult<BranchReservationFormReadDto>> UpdateReservationForm(
            BranchReservationFormUpdateDto dto);

        // ── Contact: LECTURA ──────────────────────────────────────────

        /// <summary>
        /// Devuelve los datos de contacto de la Company.
        /// Solo accesible a CompanyAdmin y roles superiores. Staff y BranchAdmin reciben error.
        /// </summary>
        Task<OperationResult<CompanyContactReadDto>> GetCompanyContact();

        /// <summary>
        /// Devuelve los datos de contacto de una Branch.
        /// Staff recibe error. BranchAdmin solo puede ver su propia Branch.
        /// </summary>
        Task<OperationResult<BranchContactReadDto>> GetBranchContact(int branchId);

        // ── Contact: ACTUALIZACIÓN ────────────────────────────────────

        Task<OperationResult<CompanyContactReadDto>> UpdateCompanyContact(CompanyContactUpdateDto dto);

        Task<OperationResult<BranchContactReadDto>> UpdateBranchContact(int branchId, BranchContactUpdateDto dto);
    }
}
