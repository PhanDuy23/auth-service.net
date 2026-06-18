using System.ComponentModel.DataAnnotations;

namespace auth_service.DTOs.Requests;

/// <summary>
/// Dùng để bật/tắt 2FA — yêu cầu xác minh mã TOTP hiện tại khi tắt
/// </summary>
public class TwoFactorToggleDto
{
    /// <summary>
    /// Mã TOTP 6 chữ số để xác nhận thao tác (bắt buộc khi tắt 2FA)
    /// </summary>
    [StringLength(6, MinimumLength = 6)]
    public string? Code { get; set; }
}
