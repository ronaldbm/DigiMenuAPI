using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface ICustomerService
    {
        Task<OperationResult<PagedResult<CustomerReadDto>>> GetAll(string? search, int page, int pageSize);
        Task<OperationResult<CustomerDetailReadDto>>        GetById(int id);
        Task<OperationResult<CustomerDetailReadDto>>        Create(CustomerCreateDto dto);
        Task<OperationResult<CustomerDetailReadDto>>        Update(CustomerUpdateDto dto);
        Task<OperationResult<CustomerDetailReadDto>>        ToggleActive(int id);
        Task<OperationResult<PagedResult<AccountReadDto>>>  GetCustomerAccounts(int customerId, int? status, int page, int pageSize);
    }
}
