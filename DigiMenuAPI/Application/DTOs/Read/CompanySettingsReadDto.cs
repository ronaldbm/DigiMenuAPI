namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>
    /// DTO compuesto para el GET general de configuración de una Company.
    /// Agrupa Info, Theme y Seo en un solo objeto para que el panel admin
    /// pueda cargar todo en una sola llamada al abrir la página de configuración.
    /// </summary>
    public record CompanySettingsReadDto(
        CompanyInfoReadDto Info,
        CompanyThemeReadDto Theme,
        CompanySeoReadDto Seo
    );
}
