using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;

namespace DigiMenuAPI.Application.Interfaces;

public interface IBranchDiscountService
{
    Task<OperationResult<List<BranchDiscountReadDto>>> GetByBranch(int branchId);
    Task<OperationResult<BranchDiscountReadDto>>       GetById(int id);
    Task<OperationResult<BranchDiscountReadDto>>       Create(BranchDiscountCreateDto dto);
    Task<OperationResult<BranchDiscountReadDto>>       Update(BranchDiscountUpdateDto dto);
    Task<OperationResult<bool>>                        ToggleActive(int id);
    Task<OperationResult<bool>>                        Delete(int id);
}
