namespace DigiMenuAPI.Application.DTOs.Create
{
    /// <summary>
    /// Creación de un día especial o feriado.
    ///
    /// Reglas:
    ///   - Date no puede ser pasada.
    ///   - Si IsClosed = true  → OpenTime y CloseTime deben ser null.
    ///   - Si IsClosed = false → OpenTime y CloseTime son obligatorios
    ///                           y CloseTime debe ser posterior a OpenTime.
    ///   - Reason es obligatorio.
    /// </summary>
    public record BranchSpecialDayCreateDto(
        int BranchId,
        DateTime Date,
        bool IsClosed,
        TimeSpan? OpenTime,
        TimeSpan? CloseTime,
        string Reason
    );
}