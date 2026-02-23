namespace DigiMenuAPI.Application.DTOs.ReadDTOs
{
    public record TagReadDto(int Id, string Name, string? Color)
    {
        public TagReadDto() : this(0, string.Empty, null) { }
    }
}