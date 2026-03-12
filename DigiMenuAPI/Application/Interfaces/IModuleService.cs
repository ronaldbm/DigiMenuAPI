using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface IModuleService
    {
        // ── SuperAdmin: catálogo de módulos ─────────────────────────
        Task<OperationResult<List<PlatformModuleReadDto>>> GetAllPlatformModules();

        // ── SuperAdmin: activaciones por empresa ────────────────────
        Task<OperationResult<List<CompanyModuleReadDto>>> GetCompanyModules(int companyId);
        Task<OperationResult<CompanyModuleReadDto>> ActivateModule(ActivateModuleDto dto);
        Task<OperationResult<bool>> DeactivateModule(int companyModuleId);
        Task<OperationResult<bool>> UpdateModuleExpiry(UpdateModuleExpiryDto dto);

        // ── Tenant: consulta propia ──────────────────────────────────
        Task<OperationResult<List<CompanyModuleReadDto>>> GetMyModules();
    }
}