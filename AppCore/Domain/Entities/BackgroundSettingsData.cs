namespace AppCore.Domain.Entities
{
    /// <summary>
    /// Configuración de la imagen de fondo del menú público.
    /// Almacenada como JSON en CompanyTheme.
    /// Solo aplica si CompanyInfo.BackgroundImageUrl != null.
    /// </summary>
    public class BackgroundSettingsData
    {
        /// <summary>Opacidad de la imagen. 0 = transparente, 100 = opaca.</summary>
        public byte Opacity { get; set; } = 100;

        /// <summary>Posición CSS. 0=center, 1=top, 2=bottom, 3=left, 4=right.</summary>
        public byte Position { get; set; } = 0;

        /// <summary>Tamaño CSS. 0=cover, 1=contain, 2=auto.</summary>
        public byte Size { get; set; } = 0;

        /// <summary>Si true, la imagen se repite en tile.</summary>
        public bool Repeat { get; set; } = false;
    }
}
