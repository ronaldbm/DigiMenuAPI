using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Create
{
    public record FooterLinkCreateDto(
        int BranchId,
        string Label,
        string Url,
        int? StandardIconId,
        string? CustomSvgContent,
        int DisplayOrder,
        bool IsVisible
    );
}
