namespace auth_service.DTOs.Responses;

/// <summary>
/// Trả về khi người dùng bắt đầu setup 2FA — chứa secret key và URI để quét QR
/// </summary>
public class TwoFactorSetupDto
{
    /// <summary>
    /// Secret key dạng Base32 — người dùng nhập thủ công nếu không quét QR được
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// otpauth:// URI để tạo QR code phía frontend
    /// </summary>
    public string QrCodeUri { get; set; } = string.Empty;
}
