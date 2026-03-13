using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface IProductService
    {
        /// <summary>Catálogo global de la empresa autenticada. CompanyId desde JWT.</summary>
        Task<OperationResult<PagedResult<ProductReadDto>>> GetAll(int page = 1, int pageSize = 20);
        Task<OperationResult<ProductReadDto>> GetById(int id);

        /// <summary>Vista completa con traducciones para el formulario de edición.</summary>
        Task<OperationResult<ProductAdminReadDto>> GetForEdit(int id);

        /// <summary>CompanyId se inyecta desde el JWT — no viene en el DTO.</summary>
        Task<OperationResult<ProductReadDto>> Create(ProductCreateDto dto);
        Task<OperationResult<bool>> Update(ProductUpdateDto dto);
        Task<OperationResult<bool>> Delete(int id);
    }
}