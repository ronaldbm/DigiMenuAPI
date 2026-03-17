namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>
    /// Vista de un día especial para el panel admin y el menú público.
    /// </summary>
    public record BranchSpecialDayReadDto(
        int Id,
        DateOnly Date,
        bool IsClosed,
        TimeSpan? OpenTime,
        TimeSpan? CloseTime,
        string Reason,
        DateTime CreatedAt
    );
}