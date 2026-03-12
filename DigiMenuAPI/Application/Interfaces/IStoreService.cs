using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Read;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface IStoreService
    {
        /// <summary>
        /// Retorna el menú público completo de una Branch.
        /// Se resuelve por companySlug + branchSlug — ambos necesarios porque
        /// Branch.Slug es único dentro de la Company, no globalmente.
        /// </summary>
        Task<OperationResult<MenuBranchDto>> GetStoreMenu(string companySlug, string branchSlug);
    }
}