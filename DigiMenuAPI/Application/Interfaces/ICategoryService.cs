using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.AddDTOs;
using DigiMenuAPI.Application.DTOs.ReadDTOs;
using DigiMenuAPI.Application.DTOs.UpdateDTOs;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface ICategoryService
    {
        Task<OperationResult<int>> Create(CategoryCreateDto categoryDto);
        Task<OperationResult<bool>> Update(CategoryUpdateDto categoryDto);
        Task<OperationResult<bool>> UpdatePosition(ItemPositionUpdate categoryDto);
        Task<OperationResult<bool>> Delete(int Id);
        Task<List<CategoryDto>> GetAll();
        Task<CategoryDto?> GetOne(int Id);
    }
}
