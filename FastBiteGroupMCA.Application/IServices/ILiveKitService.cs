using FastBiteGroupMCA.Domain.Entities.Identity;
using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.IServices;

public interface ILiveKitService
{
    /// <summary>
    /// Tạo một Access Token của LiveKit với các quyền được xác định động.
    /// </summary>
    /// <param name="user">Người dùng cần tạo token.</param>
    /// <param name="session">Phiên gọi mà người dùng sẽ tham gia.</param>
    /// <param name="userRole">Vai trò của người dùng trong nhóm (nếu có).</param>
    /// <returns>Chuỗi JWT token.</returns>
    string GenerateToken(AppUser user, VideoCallSessions session, EnumGroupRole? userRole);
}
