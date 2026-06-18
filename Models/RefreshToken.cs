namespace auth_service.Models;

public class RefreshToken
{
    public int Id { get; set; }

    /// <summary>SHA-256 hash của raw token lưu ở client</summary>
    public string TokenHash { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Null = chưa bị thu hồi</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>Token thay thế khi rotate</summary>
    public string? ReplacedByTokenHash { get; set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsRevoked && !IsExpired;
}
