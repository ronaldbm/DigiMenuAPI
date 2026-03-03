using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Add
{
    public record FooterLinkCreateDto(
        string Label,
        string Url,
        int? StandardIconId,   // Si eligen uno de nuestra lista
        string? CustomSvgContent, // Por si pegan su propio código SVG
        int DisplayOrder,
        bool IsVisible
    );
}
