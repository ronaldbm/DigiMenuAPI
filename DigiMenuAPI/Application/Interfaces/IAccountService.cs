using AppCore.Application.Common;
using DigiMenuAPI.Application.Common.Enums;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;

namespace DigiMenuAPI.Application.Interfaces;

public interface IAccountService
{
    Task<OperationResult<PagedResult<AccountReadDto>>>   GetByBranch(int branchId, AccountStatus? status, int page, int pageSize);
    Task<OperationResult<AccountDetailReadDto>>          GetById(int id);
    Task<OperationResult<AccountDetailReadDto>>          Create(AccountCreateDto dto);
    Task<OperationResult<AccountDetailReadDto>>          AddItem(AccountItemCreateDto dto);
    Task<OperationResult<AccountDetailReadDto>>          UpdateItem(AccountItemUpdateDto dto);
    Task<OperationResult<AccountDetailReadDto>>          RemoveItem(int accountItemId);
    Task<OperationResult<AccountDetailReadDto>>          ApplyDiscount(ApplyDiscountDto dto);
    Task<OperationResult<AccountDiscountReadDto>>        AuthorizeDiscount(AuthorizeDiscountDto dto);
    Task<OperationResult<AccountDetailReadDto>>          RemoveDiscount(int accountDiscountId);
    Task<OperationResult<AccountDetailReadDto>>          SetStatus(AccountStatusUpdateDto dto);
    Task<OperationResult<AccountDetailReadDto>>          CreateSplit(AccountSplitCreateDto dto);
    Task<OperationResult<AccountDetailReadDto>>          RemoveSplit(int accountSplitId);
}
