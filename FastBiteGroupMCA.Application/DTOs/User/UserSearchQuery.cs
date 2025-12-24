using FastBiteGroupMCA.Application.DTOs.Common;

namespace FastBiteGroupMCA.Application.DTOs.User
{
    public class UserSearchQuery : PaginationParams
    {
        /// <summary>
        /// thường là tìm theo usernam hoặc email
        /// </summary>
        public string? Query { get; set; } 
        public Guid? ExcludeGroupId { get; set; } // tìm trong group nếu có ko thì tìm trong toàn db
    }
}
