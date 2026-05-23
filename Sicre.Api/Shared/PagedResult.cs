namespace Sicre.Api.Shared;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalItems { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page * PageSize < TotalItems;
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}
