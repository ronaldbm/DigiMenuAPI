namespace DigiMenuAPI.Application.Common.Enums;

public enum AccountStatus : byte
{
    Open           = 1,
    PendingPayment = 2,
    Closed         = 3,
    Cancelled      = 4
}
