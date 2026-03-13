using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface IReservationService
    {
        /// <summary>
        /// Admin: reservas de la empresa autenticada.
        /// BranchAdmin/Staff ven solo su propia Branch.
        /// CompanyAdmin ve todas las Branches de su empresa.
        /// </summary>
        Task<OperationResult<PagedResult<ReservationReadDto>>> GetAll(int page = 1, int pageSize = 20);

        /// <summary>
        /// Público: el cliente crea una reserva.
        /// branchId y companyId se resuelven por Branch.Slug en el controller.
        /// </summary>
        Task<OperationResult<int>> Create(ReservationCreateDto dto, int branchId, int companyId);

        Task<OperationResult<bool>> UpdateStatus(int id, ReservationStatus newStatus);
        Task<OperationResult<bool>> Delete(int id);
    }
}