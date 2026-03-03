using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.Add;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface IProductService
    {
        Task<OperationResult<List<ProductReadDto>>> GetAll();
        Task<OperationResult<ProductReadDto>> GetById(int id);
        Task<OperationResult<ProductAdminReadDto>> GetForEdit(int id);
        Task<OperationResult<ProductReadDto>> Create(ProductCreateDto dto);
        Task<OperationResult<bool>> Update(ProductUpdateDto dto);
        Task<OperationResult<bool>> Delete(int id);
    }
}