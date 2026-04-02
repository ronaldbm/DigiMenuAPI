using DigiMenuAPI.Application.Common.Enums;

namespace DigiMenuAPI.Application.DTOs.Read;

public record AccountDiscountReadDto(
    int                   Id,
    int                   AccountId,
    int                   BranchDiscountId,
    string                DiscountName,
    int?                  AccountItemId,
    string?               ProductName,
    DiscountType          DiscountType,
    decimal               DiscountValue,
    DiscountAppliesTo     AppliesTo,
    string                Reason,
    AccountDiscountStatus Status,
    string                StatusLabel,
    int?                  AuthorizedByUserId,
    string?               AuthorizedByUserName,
    DateTime              CreatedAt
);
