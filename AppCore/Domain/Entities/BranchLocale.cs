using System.ComponentModel.DataAnnotations;

namespace AppCore.Domain.Entities
{
    /// <summary>
    /// Configuración regional de una Branch.
    /// Define el idioma, moneda, zona horaria y formato de precios
    /// que se usan tanto en el menú público como en el panel admin.
    ///
    /// Se configura una vez al crear la Branch y raramente cambia.
    /// Separada del resto para poder actualizarla sin afectar tema ni identidad.
    ///
    /// Relación 1:1 con Branch.
    /// </summary>
    public class BranchLocale : BaseEntity
    {
        // ── Multi-Tenant ─────────────────────────────────────────────
        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        // ── Localización ──────────────────────────────────────────────
        /// <summary>Código de país ISO 3166-1 alpha-2. Ejemplo: "CR", "MX", "CO".</summary>
        [Required, MaxLength(3)]
        public string CountryCode { get; set; } = "CR";

        /// <summary>Prefijo telefónico internacional. Ejemplo: "+506", "+52".</summary>
        [Required, MaxLength(6)]
        public string PhoneCode { get; set; } = "+506";

        /// <summary>Código de moneda ISO 4217. Ejemplo: "CRC", "MXN", "USD".</summary>
        [Required, MaxLength(5)]
        public string Currency { get; set; } = "CRC";

        /// <summary>Locale para formateo de precios. Ejemplo: "es-CR", "es-MX".</summary>
        [Required, MaxLength(10)]
        public string CurrencyLocale { get; set; } = "es-CR";

        /// <summary>Código de idioma BCP 47. Ejemplo: "es", "en", "pt".</summary>
        [Required, MaxLength(5)]
        public string Language { get; set; } = "es";

        /// <summary>Zona horaria IANA. Ejemplo: "America/Costa_Rica", "America/Mexico_City".</summary>
        [Required, MaxLength(50)]
        public string TimeZone { get; set; } = "America/Costa_Rica";

        /// <summary>Decimales para mostrar precios. 0 = sin decimales, 2 = estándar.</summary>
        public byte Decimals { get; set; } = 2;
    }
}
