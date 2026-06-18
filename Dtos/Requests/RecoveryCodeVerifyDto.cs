using System.ComponentModel.DataAnnotations;

namespace auth_service.DTOs.Requests;

/// <summary>
/// Xác thực bằng recovery code (dự phòng khi mất quyền truy cập authenticator app).
/// Pending token được gửi tự động qua cookie — client không cần truyền thủ công.
/// </summary>
public class RecoveryCodeVerifyDto
{
    /// <summary>
    /// Recovery code (định dạng XXXX-XXXX, không phân biệt hoa thường)
    /// </summary>
    [Required]
    public string RecoveryCode { get; set; } = string.Empty;

    /// <summary>
    /// Nhớ đăng nhập (persistent refresh token cookie)
    /// </summary>
    public bool RememberMe { get; set; } = false;
}
