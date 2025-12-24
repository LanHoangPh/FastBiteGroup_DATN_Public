using FastBiteGroupMCA.Application.IServices.Auth;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace FastBiteGroupMCA.Infastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly ISendGridClient _sendGridClient;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger, ISendGridClient sendGridClient)
    {
        _configuration = configuration;
        _logger = logger;
        _sendGridClient = sendGridClient;
    }

    public async Task SendEmailConfirmationAsync(string email, string confirmationLink)
    {
        var subject = "Xác nhận tài khoản";
        var body = $@"
            <h2>Xác nhận tài khoản</h2>
            <p>Vui lòng click vào link dưới đây để xác nhận tài khoản:</p>
            <a href='{confirmationLink}'>Xác nhận tài khoản</a>
            <p>Link này sẽ hết hạn trong vòng 24 giờ.</p>";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendTwoFactorCodeAsync(string email, string code)
    {
        var subject = "Mã xác thực 2FA";
        var body = $@"
            <h2>Mã xác thực</h2>
            <p>Mã xác thực của bạn là: <strong>{code}</strong></p>
            <p>Mã này sẽ hết hạn trong vòng 5 phút.</p>";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendPasswordResetAsync(string email, string resetLink)
    {
        var subject = "Đặt lại mật khẩu";
        var body = $@"
            <h2>Đặt lại mật khẩu</h2>
            <p>Vui lòng click vào link dưới đây để đặt lại mật khẩu:</p>
            <a href='{resetLink}'>Đặt lại mật khẩu</a>
            <p>Link này sẽ hết hạn trong vòng 1 giờ.</p>";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendWelcomeEmailAsync(string email, string firstName)
    {
        var subject = "Chào mừng bạn đến với FastBite";
        var body = $@"
            <h2>Chào mừng {firstName}!</h2>
            <p>Cảm ơn bạn đã đăng ký tài khoản tại FastBite.</p>
            <p>Chúng tôi rất vui mừng được chào đón bạn!</p>";

        await SendEmailAsync(email, subject, body);
    }
    public async Task SendPasswordResetOtpAsync(string email, string otp)
    {
        var subject = "Mã khôi phục mật khẩu của bạn";
        var body = $@"
            <h2>Khôi phục mật khẩu</h2>
            <p>Bạn đã yêu cầu đặt lại mật khẩu. Vui lòng sử dụng mã OTP dưới đây để tiếp tục:</p>
            <h3 style='font-size: 24px; letter-spacing: 2px;'><strong>{otp}</strong></h3>
            <p>Mã này sẽ hết hạn trong vòng 5 phút.</p>
            <p>Nếu bạn không yêu cầu hành động này, vui lòng bỏ qua email này.</p>";

        await SendEmailAsync(email, subject, body);
    }

    private async Task SendEmailAsync(string email, string subject, string body)
    {
        try
        {
            var apiKey = _configuration["SendGrid:ApiKey"];
            var fromEmail = _configuration["SendGrid:FromAddress"];
            var fromName = _configuration["SendGrid:FromName"];

            var from = new EmailAddress(fromEmail, fromName);
            var to = new EmailAddress(email);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, body, body);

            var response = await _sendGridClient.SendEmailAsync(msg);

            if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300)
            {
                _logger.LogInformation("Email sent successfully to {Email}", email);
            }
            else
            {
                var errorMsg = await response.Body.ReadAsStringAsync();
                _logger.LogError("Failed to send email to {Email}. StatusCode: {Code}, Error: {Error}", email, response.StatusCode, errorMsg);
                throw new InvalidOperationException("SendGrid failed to send email");
            }
        }catch (Exception ex)
        {
           _logger.LogError(ex, "Exception while sending email to {Email}", email);
            throw;
        }
    }

    public async Task SendAdminForcePasswordResetEmailAsync(string email, string fullName, string resetLink)
    {
        var subject = "Thông báo Bảo mật: Yêu cầu Đặt lại Mật khẩu cho tài khoản của bạn";
        var body = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6;'>
                <h2>Chào {fullName},</h2>
                <p>
                    Vì lý do bảo mật, một <strong>Quản trị viên</strong> của hệ thống đã kích hoạt quy trình đặt lại mật khẩu cho tài khoản của bạn.
                </p>
                <p>
                    Đây có thể là do nghi ngờ có hoạt động bất thường hoặc theo yêu cầu hỗ trợ từ bạn.
                </p>
                <p>
                    Vui lòng nhấn vào nút dưới đây để tạo mật khẩu mới. Đường link này sẽ <strong>hết hạn trong vòng 1 giờ</strong>.
                </p>
                <p style='text-align: center; margin: 30px 0;'>
                    <a href='{resetLink}' style='background-color: #007bff; color: white; padding: 15px 25px; text-decoration: none; border-radius: 5px; font-size: 16px;'>
                        Tạo Mật khẩu Mới
                    </a>
                </p>
                <p>
                    Nếu bạn không nhận ra hành động này hoặc nghi ngờ đây là một lỗi, vui lòng liên hệ ngay với đội ngũ hỗ trợ của chúng tôi.
                </p>
                <hr>
                <p style='font-size: 12px; color: #777;'>
                    Email này được gửi tự động từ hệ thống FastBite.
                </p>
            </div>";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendNotifyUserTheyWereAddedAsync(string userEmail, string userName, string groupName, Guid groupId, string systemAdminName)
    {
        var subject = $"Thông báo: Bạn đã được thêm vào nhóm {groupName}";
        // TODO: Thay "yourdomain.com" bằng domain thực tế của bạn
        var groupLink = $"https://yourdomain.com/groups/{groupId}";
        var body = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6;'>
                <h2>Chào {userName},</h2>
                <p>
                    Bạn nhận được email này vì một <strong>Quản trị viên hệ thống ({systemAdminName})</strong> đã thêm bạn vào nhóm <strong>'{groupName}'</strong>.
                </p>
                <p>
                    Bạn có thể truy cập nhóm ngay bây giờ để bắt đầu thảo luận và làm việc cùng các thành viên khác.
                </p>
                <p style='text-align: center; margin: 30px 0;'>
                    <a href='{groupLink}' style='background-color: #28a745; color: white; padding: 15px 25px; text-decoration: none; border-radius: 5px; font-size: 16px;'>
                        Truy cập Nhóm
                    </a>
                </p>
                <p>Trân trọng,<br>Đội ngũ FastBite</p>
            </div>";

        await SendEmailAsync(userEmail, subject, body);
    }

    public async Task SendNotifyAdminsUserAddedAsync(List<string> adminEmails, string newUserName, string groupName, string systemAdminName)
    {
        if (adminEmails == null || !adminEmails.Any())
        {
            return; // Không có admin nào để gửi
        }

        var subject = $"[Thông báo Quản trị] Thành viên mới trong nhóm {groupName}";
        var body = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6;'>
                <h2>Chào Quản trị viên nhóm {groupName},</h2>
                <p>
                    Đây là thông báo tự động để cho bạn biết rằng một <strong>Quản trị viên hệ thống ({systemAdminName})</strong> vừa thêm người dùng <strong>'{newUserName}'</strong> vào nhóm của bạn.
                </p>
                <p>
                    Hành động này đã được ghi lại trong hệ thống kiểm toán. Bạn không cần thực hiện thêm hành động nào.
                </p>
                <p>Trân trọng,<br>Hệ thống FastBite</p>
            </div>";

        // Gửi email đến tất cả các admin/mod của nhóm
        foreach (var email in adminEmails)
        {
            await SendEmailAsync(email, subject, body);
        }
    }

    public async Task SendTemporaryPasswordEmailAsync(string email, string fullName, string userName, string temporaryPassword, string loginLink)
    {
        var subject = "Thông tin tài khoản của bạn tại FastBite";
        // TODO: Thay "your-frontend-app.com" bằng domain thực tế của bạn

        var body = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6;'>
                <h2>Chào mừng {fullName} đến với FastBite!</h2>
                <p>
                    Một tài khoản đã được tạo cho bạn bởi Quản trị viên hệ thống. Dưới đây là thông tin đăng nhập tạm thời của bạn:
                </p>
                <div style='background-color: #f2f2f2; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <p style='margin: 5px 0;'><strong>Tên đăng nhập:</strong> {userName}</p>
                    <p style='margin: 5px 0;'><strong>Mật khẩu tạm thời:</strong> <code style='font-size: 18px; font-weight: bold;'>{temporaryPassword}</code></p>
                </div>
                <p style='color: #D32F2F; font-weight: bold;'>
                    ⚠️ VÌ LÝ DO BẢO MẬT: Bạn phải đổi mật khẩu ngay sau khi đăng nhập lần đầu tiên.
                </p>
                <p style='text-align: center; margin: 30px 0;'>
                    <a href='{loginLink}' style='background-color: #007bff; color: white; padding: 15px 25px; text-decoration: none; border-radius: 5px; font-size: 16px;'>
                        Đăng nhập ngay
                    </a>
                </p>
                <p>Trân trọng,<br>Đội ngũ FastBite</p>
            </div>";

        await SendEmailAsync(email, subject, body);
    }
}
