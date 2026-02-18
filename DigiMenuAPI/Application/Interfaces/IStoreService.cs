using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.ReadDTOs;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface IStoreService
    {
        Task<OperationResult<MenuStoreDto>> GetStoreMenu();
    }
}