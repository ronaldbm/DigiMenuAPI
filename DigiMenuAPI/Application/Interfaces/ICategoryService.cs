using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.ReadDTOs;
using DigiMenuAPI.Application.DTOs.AddDTOs;
using DigiMenuAPI.Application.DTOs.UpdateDTOs;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface ICategoryService
    {
        Task<OperationResult<List<CategoryReadDto>>> GetAll();
        Task<OperationResult<CategoryReadDto>> GetById(int id);
        Task<OperationResult<CategoryReadDto>> Create(CategoryCreateDto dto);
        Task<OperationResult<bool>> Update(CategoryUpdateDto dto);
        Task<OperationResult<bool>> Delete(int id);
    }
}