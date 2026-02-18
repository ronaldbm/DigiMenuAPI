using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.AddDTOs;
using DigiMenuAPI.Application.DTOs.ReadDTOs;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface IReservationService
    {
        Task<OperationResult<List<ReservationReadDto>>> GetAll();
        Task<OperationResult<int>> Create(ReservationCreateDto dto);
        Task<OperationResult<bool>> UpdateStatus(int id, byte newStatus);
        Task<OperationResult<bool>> Delete(int id);
    }
}
