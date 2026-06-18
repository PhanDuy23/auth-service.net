namespace auth_service.Models;

/// <summary>
/// Token tạm thời chứng minh user đã xác thực mật khẩu thành công.
/// Bắt buộc phải gửi kèm khi gọi /2fa/verify hoặc /2fa/verify-recovery.
/// Hết hạn sau 5 phút — chỉ dùng một lần.
/// </summary>
public class TwoFactorPendingToken
{
    public int Id { get; set; }

    /// <summary>SHA-256 hash của raw token</summary>
    public string TokenHash { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Thời điểm đã được dùng (null = chưa dùng)</summary>
    public DateTimeOffset? UsedAt { get; set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsUsed => UsedAt.HasValue;
    public bool IsActive => !IsExpired && !IsUsed;
}
