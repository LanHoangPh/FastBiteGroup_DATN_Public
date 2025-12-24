namespace FastBiteGroupMCA.Application.Response;
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public long TotalRecords { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalRecords / (double)PageSize);
    public PagedResult(IReadOnlyList<T> items, long totalRecords, int pageNumber, int pageSize)
    {
        Items = items;
        TotalRecords = totalRecords;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}