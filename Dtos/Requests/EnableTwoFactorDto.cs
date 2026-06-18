using System.ComponentModel.DataAnnotations;

namespace auth_service.DTOs.Requests;

/// <summary>
/// Xác nhận bật 2FA — gửi secret key (từ /2fa/setup) kèm mã TOTP hiện tại
/// </summary>
public class EnableTwoFactorDto
{
    /// <summary>
    /// Secret key Base32 nhận được từ GET /api/auth/2fa/setup
    /// </summary>
    [Required]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Mã TOTP 6 chữ số từ authenticator app sau khi quét QR
    /// </summary>
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
}
