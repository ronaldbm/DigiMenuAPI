using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.AddDTOs;
using DigiMenuAPI.Application.DTOs.ReadDTOs;
using DigiMenuAPI.Application.DTOs.UpdateDTOs;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface ISubcategoryService
    {
        Task<OperationResult<int>> Create(SubcategoryCreateDto subcategoryDto);
        Task<OperationResult<bool>> Update(SubcategoryUpdateDto subcategoryDto);
        Task<OperationResult<bool>> UpdatePosition(ItemPositionUpdate subcategoryDto);
        Task<OperationResult<bool>> Delete(int Id);
        Task<List<SubcategoryDto>> GetAll();
        Task<SubcategoryDto?> GetOne(int Id);
    }
}
