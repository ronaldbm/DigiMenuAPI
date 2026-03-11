using DigiMenuAPI.Infrastructure.Entities;

namespace DigiMenuAPI.Application.Utils
{
    /// <summary>
    /// Genera los 7 registros de BranchSchedule con defaults razonables
    /// para una Branch recién creada.
    ///
    /// Defaults:
    ///   Lunes a Sábado → IsOpen=true, OpenTime=09:00, CloseTime=22:00
    ///   Domingo        → IsOpen=false
    ///
    /// El admin puede editar cada día desde el panel de configuración.
    /// Usado en AuthService.RegisterCompany y en cualquier servicio
    /// que cree una Branch nueva.
    /// </summary>
    public static class BranchScheduleInitializer
    {
        private static readonly TimeSpan DefaultOpen = new(9, 0, 0);
        private static readonly TimeSpan DefaultClose = new(22, 0, 0);

        /// <summary>
        /// Genera la lista de 7 BranchSchedule para el branchId indicado.
        /// Los registros deben agregarse al DbContext y guardarse con SaveChangesAsync.
        /// </summary>
        public static List<BranchSchedule> Generate(int branchId)
        {
            var schedules = new List<BranchSchedule>();

            for (byte day = 0; day <= 6; day++)
            {
                // DayOfWeek 0 = Domingo — cerrado por defecto
                var isOpen = day != 0;

                schedules.Add(new BranchSchedule
                {
                    BranchId = branchId,
                    DayOfWeek = day,
                    IsOpen = isOpen,
                    OpenTime = isOpen ? DefaultOpen : null,
                    CloseTime = isOpen ? DefaultClose : null
                });
            }

            return schedules;
        }
    }
}