using DigiMenuAPI.Application.Common.Enums;

namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>KPIs de cuentas para el dashboard de administración.</summary>
    public record AccountKpiDto(
        int OpenAccounts,
        int TabAccounts,
        decimal TabTotal,
        decimal RevenueToday,
        decimal RevenuePeriod,
        int PendingDiscounts,
        decimal TotalDiscountsApplied,
        int AccountsClosedToday,
        int AccountsClosedPeriod,
        decimal AverageTicket);

    /// <summary>Resumen de cuenta para la tabla de reportes.</summary>
    public record AccountReportRowDto(
        int Id,
        int BranchId,
        string BranchName,
        string ClientIdentifier,
        AccountStatus Status,
        string StatusLabel,
        int? CustomerId,
        string? CustomerName,
        decimal Subtotal,
        decimal TotalDiscounts,
        decimal Total,
        int ItemCount,
        DateTime CreatedAt,
        DateTime? ClosedAt);

    /// <summary>Estado de cuenta de un cliente.</summary>
    public record CustomerStatementDto(
        int CustomerId,
        string CustomerName,
        decimal CreditLimit,
        decimal CurrentBalance,
        decimal TotalSpentPeriod,
        List<CustomerStatementLineDto> Lines);

    public record CustomerStatementLineDto(
        int AccountId,
        string ClientIdentifier,
        AccountStatus Status,
        string StatusLabel,
        decimal Total,
        DateTime CreatedAt,
        DateTime? ClosedAt);

    /// <summary>Datos de recibo para impresión (incluye info de empresa/sucursal).</summary>
    public record AccountReceiptDto(
        string BusinessName,
        string? BranchName,
        string? BranchAddress,
        string? BranchPhone,
        int AccountId,
        string ClientIdentifier,
        string? CustomerName,
        string StatusLabel,
        DateTime CreatedAt,
        DateTime? ClosedAt,
        List<ReceiptItemDto> Items,
        List<ReceiptDiscountDto> Discounts,
        decimal Subtotal,
        decimal TotalDiscounts,
        decimal Total,
        string? CurrencySymbol);

    public record ReceiptItemDto(
        string ProductName,
        int Quantity,
        decimal UnitPrice,
        decimal LineTotal);

    public record ReceiptDiscountDto(
        string Name,
        string Description,
        decimal Amount);
}
