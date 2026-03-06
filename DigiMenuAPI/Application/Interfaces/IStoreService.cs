using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.Read;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface IStoreService
    {
        Task<OperationResult<MenuBranchDto>> GetStoreMenu(string slug);
    }
}