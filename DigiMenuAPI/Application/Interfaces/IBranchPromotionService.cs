using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;

namespace DigiMenuAPI.Application.Interfaces
{
    /// <summary>CRUD de promociones de sucursal para el carrusel.</summary>
    public interface IBranchPromotionService
    {
        Task<OperationResult<List<BranchPromotionReadDto>>> GetByBranch(int branchId);
        Task<OperationResult<BranchPromotionReadDto>> GetById(int id);
        Task<OperationResult<BranchPromotionReadDto>> Create(BranchPromotionCreateDto dto);
        Task<OperationResult<BranchPromotionReadDto>> Update(BranchPromotionUpdateDto dto);
        Task<OperationResult<bool>> Delete(int id);
        Task<OperationResult<bool>> Reorder(int branchId, List<ReorderItemDto> items);
    }
}
