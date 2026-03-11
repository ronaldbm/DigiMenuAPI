namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>
    /// Horario de un día de la semana para el panel admin y el menú público.
    /// DayOfWeek sigue la convención .NET: 0=Domingo … 6=Sábado.
    /// DayName viene resuelto en español desde el servicio.
    /// </summary>
    public record BranchScheduleReadDto(
        int Id,
        byte DayOfWeek,
        string DayName,       // "Lunes", "Martes"... resuelto en el servicio
        bool IsOpen,
        TimeSpan? OpenTime,
        TimeSpan? CloseTime
    );
}