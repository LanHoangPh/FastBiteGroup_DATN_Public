namespace FastBiteGroupMCA.Application.Response;

// ---- Đặt trong tầng Domain hoặc Application/Common ----
public static class ErrorCodes
{
    #region Lỗi chung (General Errors)
    /// <summary>
    /// Sử dụng khi có một hoặc nhiều lỗi validation từ input của người dùng.
    /// </summary>
    public const string ValidationFailed = "GENERAL.VALIDATION_FAILED";

    /// <summary>
    /// Sử dụng khi request yêu cầu xác thực nhưng không có token hợp lệ.
    /// Trả về HTTP 401 Unauthorized.
    /// </summary>
    public const string Unauthorized = "GENERAL.UNAUTHORIZED";

    /// <summary>
    /// Sử dụng khi người dùng đã được xác thực nhưng không có quyền thực hiện hành động.
    /// Trả về HTTP 403 Forbidden.
    /// </summary>
    public const string Forbidden = "GENERAL.FORBIDDEN";

    /// <summary>
    /// Sử dụng khi một tài nguyên không được tìm thấy. Nên được dùng kèm với message cụ thể.
    /// Trả về HTTP 404 Not Found.
    /// </summary>
    public const string NotFound = "GENERAL.NOT_FOUND";

    /// <summary>
    /// Sử dụng khi hai người dùng cố gắng cập nhật cùng một tài nguyên, gây ra xung đột.
    /// Trả về HTTP 409 Conflict.
    /// </summary>
    public const string ConcurrencyError = "GENERAL.CONCURRENCY_ERROR";

    /// <summary>
    /// Sử dụng cho các lỗi không mong muốn từ server.
    /// Trả về HTTP 500 Internal Server Error.
    /// </summary>
    public const string UnhandledError = "GENERAL.UNHANDLED_ERROR";
    #endregion

    #region Lỗi Xác thực (Authentication)
    /// <summary>
    /// Sử dụng khi người dùng cung cấp thông tin đăng nhập (email/password) không chính xác.
    /// </summary>
    public const string InvalidCredentials = "AUTH.INVALID_CREDENTIALS";

    /// <summary>
    /// Sử dụng khi đăng ký một email đã tồn tại trong hệ thống.
    /// </summary>
    public const string EmailAlreadyExists = "AUTH.EMAIL_ALREADY_EXISTS";

    /// <summary>
    /// Sử dụng khi Access Token (JWT) đã hết hạn.
    /// </summary>
    public const string AccessTokenExpired = "AUTH.ACCESS_TOKEN_EXPIRED";

    /// <summary>
    /// Sử dụng khi Refresh Token không hợp lệ, hết hạn, hoặc đã bị thu hồi.
    /// </summary>
    public const string RefreshTokenInvalid = "AUTH.REFRESH_TOKEN_INVALID";

    /// <summary>
    /// Sử dụng khi token xác thực email không hợp lệ hoặc hết hạn.
    /// </summary>
    public const string EmailConfirmationTokenInvalid = "AUTH.EMAIL_CONFIRMATION_TOKEN_INVALID";
    #endregion

    #region Lỗi Người dùng (User)
    /// <summary>
    /// Sử dụng khi không tìm thấy một người dùng với ID hoặc email cung cấp.
    /// </summary>
    public const string UserNotFound = "USER.NOT_FOUND";

    /// <summary>
    /// Sử dụng khi một tài khoản đang cố gắng thực hiện hành động nhưng đã bị vô hiệu hóa.
    /// </summary>
    public const string AccountDeactivated = "USER.ACCOUNT_DEACTIVATED";
    #endregion

    #region Lỗi Nhóm & Cộng đồng (Group)
    /// <summary>
    /// Sử dụng khi không tìm thấy một nhóm/cộng đồng với ID cung cấp.
    /// </summary>
    public const string GroupNotFound = "GROUP.NOT_FOUND";

    /// <summary>
    /// Sử dụng khi người dùng cố gắng tham gia một nhóm mà họ đã là thành viên.
    /// </summary>
    public const string UserAlreadyInGroup = "GROUP.ALREADY_A_MEMBER";

    /// <summary>
    /// Sử dụng khi một người cố gắng thực hiện hành động (đăng bài) trong nhóm mà họ không phải là thành viên.
    /// </summary>
    public const string UserNotAMember = "GROUP.NOT_A_MEMBER";
    #endregion

    #region Lỗi Bài đăng & Bình luận (Post & Comment)
    /// <summary>
    /// Sử dụng khi không tìm thấy một bài đăng với ID cung cấp.
    /// </summary>
    public const string PostNotFound = "POST.NOT_FOUND";

    /// <summary>
    /// Sử dụng khi không tìm thấy một bình luận với ID cung cấp.
    /// </summary>
    public const string CommentNotFound = "COMMENT.NOT_FOUND";
    #endregion

    #region Lỗi Link mời (Invitation)
    /// <summary>
    /// Sử dụng khi mã mời không tồn tại.
    /// </summary>
    public const string InvitationCodeInvalid = "INVITATION.INVALID_CODE";

    /// <summary>
    /// Sử dụng khi link mời đã hết hạn.
    /// </summary>
    public const string InvitationExpired = "INVITATION.EXPIRED";

    /// <summary>
    /// Sử dụng khi link mời đã hết lượt sử dụng.
    /// </summary>
    public const string InvitationUsageLimitReached = "INVITATION.LIMIT_REACHED";
    #endregion
    public const string UpdateFailed = "UPDATE_FAILED";
    public const string CreateFailed = "CREATE_FAILED";
    public const string DeleteFailed = "DELETE_FAILED";
    //public const string GroupNotFound = "GROUP_NOT_FOUND";
}