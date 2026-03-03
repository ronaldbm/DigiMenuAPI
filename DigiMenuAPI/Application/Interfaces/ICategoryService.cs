using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Add;
using DigiMenuAPI.Application.DTOs.Update;

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