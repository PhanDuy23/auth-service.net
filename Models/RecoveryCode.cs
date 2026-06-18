namespace auth_service.Models;

/// <summary>
/// Recovery codes dự phòng khi người dùng mất quyền truy cập vào authenticator app
/// </summary>
public class RecoveryCode
{
    public int Id { get; set; }

    /// <summary>
    /// SHA-256 hash của recovery code — không bao giờ lưu raw code
    /// </summary>
    public string CodeHash { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key tới ApplicationUser
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Thời điểm tạo code
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Thời điểm code được sử dụng (null = chưa dùng)
    /// </summary>
    public DateTimeOffset? UsedAt { get; set; }

    /// <summary>
    /// Code đã được sử dụng?
    /// </summary>
    public bool IsUsed => UsedAt.HasValue;
}
