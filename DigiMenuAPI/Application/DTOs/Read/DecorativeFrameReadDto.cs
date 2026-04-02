namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>Metadata de un marco decorativo predefinido (sin SVG content para el listado).</summary>
    public record DecorativeFrameReadDto(
        int Id,
        string Name,
        string Category,
        int DisplayOrder
    );
}
