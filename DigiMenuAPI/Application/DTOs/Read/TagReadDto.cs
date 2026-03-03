namespace DigiMenuAPI.Application.DTOs.Read
{
    public record TagReadDto(int Id, string Name, string? Color)
    {
        public TagReadDto() : this(0, string.Empty, null) { }
    }
}