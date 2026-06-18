namespace auth_service.DTOs.Responses;

public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = [];

    /// <summary>
    /// true khi tài khoản đã bật 2FA.
    /// Client gọi tiếp POST /api/auth/2fa/verify hoặc /2fa/verify-recovery.
    /// Pending token được set tự động qua HttpOnly cookie — client không cần xử lý.
    /// </summary>
    public bool RequiresTwoFactor { get; set; } = false;

    /// <summary>
    /// true sau khi dùng recovery code để đăng nhập.
    /// Client nên redirect người dùng đến màn hình cấu hình lại 2FA ngay.
    /// </summary>
    public bool RequiresTwoFactorReconfiguration { get; set; } = false;
}
