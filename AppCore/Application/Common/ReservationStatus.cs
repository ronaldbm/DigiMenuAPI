namespace AppCore.Application.Common
{
    /// <summary>
    /// Estados posibles de una reserva.
    /// Se almacena como tinyint en la base de datos.
    /// </summary>
    public enum ReservationStatus : byte
    {
        Pending   = 1,
        Confirmed = 2,
        Cancelled = 3,
        Completed = 4
    }
}
