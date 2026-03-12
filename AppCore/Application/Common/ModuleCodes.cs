namespace AppCore.Application.Common
{
    /// <summary>
    /// Códigos únicos de los módulos de la plataforma.
    /// Deben coincidir exactamente con el campo <c>Code</c> de la tabla <c>PlatformModules</c>
    /// y con el seed de datos inicial.
    /// </summary>
    public static class ModuleCodes
    {
        public const string Reservations = "RESERVATIONS";
        public const string TableManagement = "TABLE_MANAGEMENT";
        public const string Analytics = "ANALYTICS";
        public const string OnlineOrders = "ONLINE_ORDERS";
    }
}
