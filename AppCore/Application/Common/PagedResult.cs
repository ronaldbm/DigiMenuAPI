namespace AppCore.Application.Common
{
    /// <summary>
    /// Resultado paginado genérico.
    /// Devuelto por todos los endpoints de lista que pueden crecer en tamaño.
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Data { get; init; } = new();
        public int Total { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 0;
        public bool HasNext => Page < TotalPages;
        public bool HasPrev => Page > 1;

        public static PagedResult<T> Create(List<T> data, int total, int page, int pageSize)
            => new() { Data = data, Total = total, Page = page, PageSize = pageSize };
    }
}
