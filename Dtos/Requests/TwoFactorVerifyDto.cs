using System.ComponentModel.DataAnnotations;

namespace auth_service.DTOs.Requests;

/// <summary>
/// Dùng để xác thực mã TOTP sau khi login khi 2FA đang bật.
/// Pending token được gửi tự động qua cookie — client không cần truyền thủ công.
/// </summary>
public class TwoFactorVerifyDto
{
    /// <summary>
    /// Mã TOTP 6 chữ số từ authenticator app
    /// </summary>
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Nhớ đăng nhập (persistent refresh token cookie)
    /// </summary>
    public bool RememberMe { get; set; } = false;
}
