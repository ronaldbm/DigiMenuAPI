using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;

namespace DigiMenuAPI.Application.Interfaces
{
    /// <summary>
    /// Gestión del horario semanal y días especiales de una Branch.
    ///
    /// Es independiente del módulo RESERVATIONS — el horario es
    /// información del negocio visible para cualquier cliente en el menú público.
    ///
    /// BranchSchedule:
    ///   - Siempre 7 registros por Branch, generados al crear la Branch.
    ///   - El admin solo actualiza, nunca crea ni elimina filas.
    ///
    /// BranchSpecialDay:
    ///   - El admin crea/edita/elimina días especiales (feriados, eventos, cierres).
    ///   - No se permiten fechas pasadas al crear.
    ///   - Una Branch no puede tener dos registros para la misma fecha.
    ///   - Se conservan indefinidamente como historial.
    ///   - Eliminación física (no hay soft delete).
    /// </summary>
    public interface IScheduleService
    {
        // ── Horario semanal ───────────────────────────────────────────

        /// <summary>
        /// Devuelve los 7 días del horario semanal ordenados Lun-Dom.
        /// </summary>
        Task<OperationResult<List<BranchScheduleReadDto>>> GetSchedule(int branchId);

        /// <summary>
        /// Actualiza el horario de un día específico.
        /// Si IsOpen = false, OpenTime y CloseTime se guardan como null.
        /// </summary>
        Task<OperationResult<BranchScheduleReadDto>> UpdateScheduleDay(
            BranchScheduleUpdateDto dto);

        // ── Días especiales ───────────────────────────────────────────

        /// <summary>
        /// Devuelve todos los días especiales ordenados por fecha ascendente.
        /// Incluye fechas pasadas (historial).
        /// </summary>
        Task<OperationResult<List<BranchSpecialDayReadDto>>> GetSpecialDays(int branchId);

        /// <summary>
        /// Crea un día especial. La fecha no puede ser pasada.
        /// </summary>
        Task<OperationResult<BranchSpecialDayReadDto>> CreateSpecialDay(
            BranchSpecialDayCreateDto dto);

        /// <summary>
        /// Edita un día especial. La fecha no se puede cambiar.
        /// </summary>
        Task<OperationResult<BranchSpecialDayReadDto>> UpdateSpecialDay(
            BranchSpecialDayUpdateDto dto);

        /// <summary>Elimina un día especial (eliminación física).</summary>
        Task<OperationResult<bool>> DeleteSpecialDay(int specialDayId);
    }
}