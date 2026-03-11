namespace DigiMenuAPI.Application.DTOs.Update
{
    /// <summary>
    /// Actualización del horario de UN día específico de la semana.
    /// Si IsOpen = false, OpenTime y CloseTime se ignoran y se guardan como null.
    /// Si IsOpen = true, ambos son obligatorios y CloseTime debe ser posterior a OpenTime.
    /// </summary>
    public record BranchScheduleUpdateDto(
        int BranchId,
        byte DayOfWeek,
        bool IsOpen,
        TimeSpan? OpenTime,
        TimeSpan? CloseTime
    );
}