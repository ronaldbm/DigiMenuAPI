namespace DigiMenuAPI.Application.DTOs.Update
{
    /// <summary>
    /// Actualización de un día especial existente.
    /// La fecha no se puede cambiar — eliminar y recrear si cambia la fecha.
    /// </summary>
    public record BranchSpecialDayUpdateDto(
        int Id,
        int BranchId,
        bool IsClosed,
        TimeSpan? OpenTime,
        TimeSpan? CloseTime,
        string Reason
    );
}