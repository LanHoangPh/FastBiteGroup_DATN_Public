using FastBiteGroupMCA.Application.DTOs.Common;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.Invitation;

public class GetSentInvitationsQuery : PaginationParams
{
    /// <summary>
    /// Lọc lời mời theo trạng thái (tùy chọn).
    /// </summary>
    public EnumInvitationStatus? Status { get; set; }

    /// <summary>
    /// Tìm kiếm theo tên của người được mời (tùy chọn).
    /// </summary>
    public string? SearchTerm { get; set; }
}
