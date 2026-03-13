namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>
    /// DTO compuesto para el GET general de configuración de una Branch.
    /// Agrupa las secciones de nivel Branch en un solo objeto para que el panel admin
    /// pueda cargar todo en una sola llamada al abrir la página de configuración.
    ///
    /// ReservationForm es null si el módulo RESERVATIONS no está activo.
    /// Info, Theme y Seo han sido movidas a CompanySettingsReadDto (nivel Company).
    /// </summary>
    public record BranchSettingsReadDto(
        BranchLocaleReadDto Locale,
        BranchReservationFormReadDto? ReservationForm
    );
}
