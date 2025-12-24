namespace FastBiteGroupMCA.Application.IServices.Auth
{
    public interface IOtpService
    {
        Task<string> GenerateOtpAsync(string key);
        Task<bool> ValidateOtpAsync(string key, string otp);
        Task InvalidateOtpAsync(string key);
        Task<int> GetFailedAttemptsAsync(string key);
        Task IncrementFailedAttemptsAsync(string key);
        Task ResetFailedAttemptsAsync(string key);
    }
}
