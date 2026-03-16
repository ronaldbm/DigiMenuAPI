namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>Conteos globales de la empresa para el dashboard del admin.</summary>
    public record DashboardStatsDto(
        int Products,
        int Categories,
        int Tags,
        int Users);
}
