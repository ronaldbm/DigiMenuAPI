namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>
    /// DTO compuesto para el GET general de configuración de una Branch.
    /// Agrupa las 5 secciones en un solo objeto para que el panel admin
    /// pueda cargar todo en una sola llamada al abrir la página de configuración.
    ///
    /// ReservationForm es null si el módulo RESERVATIONS no está activo.
    /// </summary>
    public record BranchSettingsReadDto(
        BranchInfoReadDto Info,
        BranchThemeReadDto Theme,
        BranchLocaleReadDto Locale,
        BranchSeoReadDto Seo,
        BranchReservationFormReadDto? ReservationForm
    );
}