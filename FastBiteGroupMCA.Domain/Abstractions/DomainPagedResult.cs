namespace FastBiteGroupMCA.Domain.Abstractions;

/// <summary>
/// Một cấu trúc dữ liệu đơn giản trong tầng Domain để chứa kết quả phân trang từ Repository.
/// </summary>
public class DomainPagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public long TotalRecords { get; }

    public DomainPagedResult(IReadOnlyList<T> items, long totalRecords)
    {
        Items = items;
        TotalRecords = totalRecords;
    }
}