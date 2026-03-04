namespace DigiMenuAPI.Application.DTOs.Create
{
    public record ProductCreateDto(
        int CompanyId,
        int CategoryId,
        string Name,
        string? ShortDescription,
        string? LongDescription,
        IFormFile? Image,
        List<int>? TagIds
    );
}
