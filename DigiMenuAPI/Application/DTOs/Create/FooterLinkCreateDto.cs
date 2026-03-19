using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Create
{
    public record FooterLinkCreateDto(
        int BranchId,
        [Required, MaxLength(100)] string Label,
        [Required, MaxLength(500)] string Url,
        int? StandardIconId,
        string? CustomSvgContent,
        int DisplayOrder,
        bool IsVisible
    );
}
