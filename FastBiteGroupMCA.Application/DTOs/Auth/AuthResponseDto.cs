using FastBiteGroupMCA.Application.DTOs.User;

namespace FastBiteGroupMCA.Application.DTOs.Auth
{
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool RequiresTwoFactor { get; set; } = false;
        public UserDto User { get; set; } = null!;
    }
}
