using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.ReadDTOs;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface IStandardIconService
    {
        Task<OperationResult<List<StandardIconReadDto>>> GetAll();
    }
}
