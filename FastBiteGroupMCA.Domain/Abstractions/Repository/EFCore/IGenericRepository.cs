using System.Linq.Expressions;

namespace FastBiteGroupMCA.Domain.Abstractions.Repository.EFCore;

public interface IGenericRepository<TEnity> where TEnity : class
{
    Task<TEnity?> GetByIdAsync(object id);
    // Lấy tất cả các đối tượng
    Task<IEnumerable<TEnity>> GetAllAsync();
    // Tìm kiếm các đối tượng theo một điều kiện cụ thể
    Task<IEnumerable<TEnity>> FindAsync(Expression<Func<TEnity, bool>> predicate);
    // Thêm một đối tượng mới
    Task AddAsync(TEnity entity);
    // Thêm một danh sách các đối tượng
    Task AddRangeAsync(IEnumerable<TEnity> entities);
    // Xóa một đối tượng
    void Remove(TEnity entity);
    // Xóa một danh sách các đối tượng
    void RemoveRange(IEnumerable<TEnity> entities);
    // Cập nhật một đối tượng (EF Core theo dõi thay đổi nên thường không cần)
    void Update(TEnity entity);
    IQueryable<TEnity> GetQueryable();
}
