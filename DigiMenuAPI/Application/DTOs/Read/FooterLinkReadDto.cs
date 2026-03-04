public record FooterLinkReadDto(
    int Id,
    int BranchId,
    string Label,
    string Url,
    string SvgContent,   
    int DisplayOrder,
    bool IsVisible
);