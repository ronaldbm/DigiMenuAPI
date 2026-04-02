using DigiMenuAPI.Application.Common.Enums;

namespace DigiMenuAPI.Application.DTOs.Read;

public record BranchDiscountReadDto(
    int              Id,
    int              BranchId,
    string           Name,
    DiscountType     DiscountType,
    string           DiscountTypeName,
    decimal          DefaultValue,
    DiscountAppliesTo AppliesTo,
    string           AppliesToName,
    bool             RequiresApproval,
    decimal?         MaxValueForStaff,
    bool             IsActive,
    DateTime         CreatedAt
);
