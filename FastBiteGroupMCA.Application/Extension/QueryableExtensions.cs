using FastBiteGroupMCA.Application.Response;
using Microsoft.EntityFrameworkCore;

namespace FastBiteGroupMCA.Application.Extension;
public static class QueryableExtensions
{
    /// <summary>
    /// Phương thức mở rộng cho IQueryable để thực hiện phân trang.
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu của các phần tử.</typeparam>
    /// <param name="queryable">IQueryable cần phân trang.</param>
    /// <param name="pageNumber">Số trang hiện tại.</param>
    /// <param name="pageSize">Số lượng phần tử trên mỗi trang.</param>
    /// <returns>Một đối tượng PagedResult chứa dữ liệu của trang hiện tại.</returns>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> queryable,
        int pageNumber,
        int pageSize)
    {
        // Đếm tổng số lượng bản ghi (trước khi áp dụng Skip/Take)
        var totalRecords = await queryable.CountAsync();

        // Lấy danh sách các mục cho trang hiện tại
        var items = await queryable
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<T>(items, totalRecords, pageNumber, pageSize);
    }
}
