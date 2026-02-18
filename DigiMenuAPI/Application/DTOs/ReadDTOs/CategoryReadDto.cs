namespace DigiMenuAPI.Application.DTOs.ReadDTOs
{
    public record CategoryReadDto(int Id, string Name, int DisplayOrder, List<ProductReadDto> Products)
    {
        public CategoryReadDto() : this(0, string.Empty, 0, new List<ProductReadDto>()) { }
    }
}