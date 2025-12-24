namespace FastBiteGroupMCA.Application.IServices.Auth
{
    public interface IEmailService
    {
        Task SendEmailConfirmationAsync(string email, string confirmationLink);
        Task SendTwoFactorCodeAsync(string email, string code);
        Task SendPasswordResetAsync(string email, string resetLink);
        Task SendWelcomeEmailAsync(string email, string firstName);
        Task SendPasswordResetOtpAsync(string email, string otp);
        /// <summary>
        /// Gửi email cho người dùng khi Admin buộc họ phải đặt lại mật khẩu.
        /// </summary>
        /// <param name="email">Email người nhận.</param>
        /// <param name="fullName">Tên đầy đủ của người dùng để cá nhân hóa.</param>
        /// <param name="resetLink">Đường link đặt lại mật khẩu duy nhất.</param>
        Task SendAdminForcePasswordResetEmailAsync(string email, string fullName, string resetLink);
        // THÊM 2 PHƯƠNG THỨC MỚI

        /// <summary>
        /// Gửi email cho người dùng thông báo rằng họ vừa được Admin tổng thêm vào một nhóm.
        /// </summary>
        Task SendNotifyUserTheyWereAddedAsync(string userEmail, string userName, string groupName, Guid groupId, string systemAdminName);

        /// <summary>
        /// Gửi email cho các Admin/Mod của một nhóm để thông báo có thành viên mới được thêm bởi Admin tổng.
        /// </summary>
        Task SendNotifyAdminsUserAddedAsync(List<string> adminEmails, string newUserName, string groupName, string systemAdminName);
        /// <summary>
        /// Gửi email chứa thông tin tài khoản và mật khẩu tạm thời cho người dùng mới.
        /// </summary>
        Task SendTemporaryPasswordEmailAsync(string email, string fullName, string userName, string temporaryPassword, string loginLink);
    }
}
