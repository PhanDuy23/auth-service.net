namespace auth_service.DTOs.Responses;

/// <summary>
/// Trả về khi login thành công nhưng cần xác thực 2FA tiếp theo
/// </summary>
public class LoginPendingTwoFactorDto
{
    /// <summary>
    /// Luôn là true — báo hiệu client cần chuyển sang màn hình nhập mã 2FA
    /// </summary>
    public bool RequiresTwoFactor { get; set; } = true;

    /// <summary>
    /// Email của user — dùng để gọi endpoint verify-2fa
    /// </summary>
    public string Email { get; set; } = string.Empty;
}
