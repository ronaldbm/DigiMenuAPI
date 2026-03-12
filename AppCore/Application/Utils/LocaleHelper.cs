namespace AppCore.Application.Utils
{
    /// <summary>
    /// Helpers de resolución regional por código de país ISO 3166-1 alpha-2.
    ///
    /// Devuelven valores por defecto razonables para los países objetivo del SaaS.
    /// El administrador puede ajustar cualquier valor desde BranchLocale en cualquier momento.
    ///
    /// Usado en:
    ///   - AuthService.RegisterCompany  → inicializa la branch principal
    ///   - BranchService.Create         → inicializa branches adicionales
    /// </summary>
    public static class LocaleHelper
    {
        /// <summary>Código telefónico internacional. Ejemplo: "CR" → "+506"</summary>
        public static string ResolvePhoneCode(string? countryCode) =>
            countryCode?.ToUpper() switch
            {
                "MX" => "+52",
                "CO" => "+57",
                "US" => "+1",
                "GT" => "+502",
                "PA" => "+507",
                "SV" => "+503",
                "HN" => "+504",
                "NI" => "+505",
                _ => "+506"
            };

        /// <summary>Código de moneda ISO 4217. Ejemplo: "CR" → "CRC"</summary>
        public static string ResolveCurrency(string? countryCode) =>
            countryCode?.ToUpper() switch
            {
                "MX" => "MXN",
                "CO" => "COP",
                "US" => "USD",
                "GT" => "GTQ",
                "PA" => "USD",
                "SV" => "USD",
                "HN" => "HNL",
                "NI" => "NIO",
                _ => "CRC"
            };

        /// <summary>Locale para formateo de moneda. Ejemplo: "CR" → "es-CR"</summary>
        public static string ResolveCurrencyLocale(string? countryCode) =>
            countryCode?.ToUpper() switch
            {
                "MX" => "es-MX",
                "CO" => "es-CO",
                "US" => "en-US",
                "GT" => "es-GT",
                "PA" => "es-PA",
                "SV" => "es-SV",
                "HN" => "es-HN",
                "NI" => "es-NI",
                _ => "es-CR"
            };

        /// <summary>Zona horaria IANA. Ejemplo: "CR" → "America/Costa_Rica"</summary>
        public static string ResolveTimeZone(string? countryCode) =>
            countryCode?.ToUpper() switch
            {
                "MX" => "America/Mexico_City",
                "CO" => "America/Bogota",
                "US" => "America/New_York",
                "GT" => "America/Guatemala",
                "PA" => "America/Panama",
                "SV" => "America/El_Salvador",
                "HN" => "America/Tegucigalpa",
                "NI" => "America/Managua",
                _ => "America/Costa_Rica"
            };
    }
}
