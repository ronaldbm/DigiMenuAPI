using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface IReservationService
    {
        Task<OperationResult<List<ReservationReadDto>>> GetAll();
        Task<OperationResult<int>> Create(ReservationCreateDto dto, int companyId);
        Task<OperationResult<bool>> UpdateStatus(int id, byte newStatus);
        Task<OperationResult<bool>> Delete(int id);
    }
}