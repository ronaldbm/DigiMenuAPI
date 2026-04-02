namespace AppCore.Domain.Entities
{
    /// <summary>
    /// Configuración del marco decorativo del menú público.
    /// Almacenada como JSON en CompanyTheme.
    /// FrameId=0 significa sin marco.
    /// </summary>
    public class FrameSettingsData
    {
        /// <summary>0=ninguno, 1-8=predefinido, 255=custom subido por el usuario.</summary>
        public byte FrameId { get; set; } = 0;

        /// <summary>URL del SVG custom. Solo aplica cuando FrameId=255.</summary>
        public string? CustomFrameUrl { get; set; }
    }
}
