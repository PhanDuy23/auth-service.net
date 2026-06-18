using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using auth_service.Authorization;
using auth_service.Data;
using auth_service.Models;
using auth_service.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace auth_service.Services;

/// <summary>
/// Internal helper service — tạo JWT, tạo/hash refresh token.
/// Không expose interface public vì chỉ dùng nội bộ giữa các service.
/// </summary>
public class TokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ApplicationDbContext _db;

    public TokenService(IOptions<JwtSettings> jwtSettings, ApplicationDbContext db)
    {
        _jwtSettings = jwtSettings.Value;
        _db = db;
    }

    // ── JWT ───────────────────────────────────────────────────────────────────

    public (string token, DateTime expiresAt) GenerateJwtToken(
        ApplicationUser user,
        IList<string> roles
    )
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("fullName", user.FullName ?? string.Empty),
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        // Gán permission claims dựa theo role
        var permissions = ResolvePermissions(roles);
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    /// <summary>
    /// Map role → danh sách permissions được cấp.
    /// Admin kế thừa toàn bộ permissions của Employee.
    /// </summary>
    private static IEnumerable<string> ResolvePermissions(IList<string> roles)
    {
        var permissions = new HashSet<string>();

        if (roles.Contains("Employee") || roles.Contains("Admin"))
        {
            permissions.Add(Permissions.UsersRead);
        }

        if (roles.Contains("Admin"))
        {
            permissions.Add(Permissions.UsersDelete);
        }

        return permissions;
    }

    // ── Refresh Token ─────────────────────────────────────────────────────────

    /// <summary>
    /// Tạo refresh token mới, lưu hash vào DB, trả về raw token cho client.
    /// </summary>
    public async Task<(string rawToken, RefreshToken entity)> CreateRefreshTokenAsync(string userId)
    {
        // 64 bytes ngẫu nhiên → base64url (không có padding)
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var rawToken = Convert
            .ToBase64String(randomBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        var entity = new RefreshToken
        {
            TokenHash = HashToken(rawToken),
            UserId = userId,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(_jwtSettings.RefreshTokenExpiryHours),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync();

        return (rawToken, entity);
    }

    /// <summary>SHA-256 hash của raw token — không bao giờ lưu raw token vào DB.</summary>
    public static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    // ── 2FA Pending Token ─────────────────────────────────────────────────────

    /// <summary>
    /// Tạo pending token chứng minh user đã xác thực mật khẩu thành công.
    /// Token hết hạn sau 5 phút và chỉ dùng được một lần.
    /// </summary>
    public async Task<string> CreatePendingTokenAsync(string userId)
    {
        // Xóa các pending tokens cũ của user (expired hoặc used) trước khi tạo mới
        var stale = _db.TwoFactorPendingTokens.Where(t =>
            t.UserId == userId && (t.UsedAt != null || t.ExpiresAt <= DateTimeOffset.UtcNow)
        );
        _db.TwoFactorPendingTokens.RemoveRange(stale);

        var rawBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Convert
            .ToBase64String(rawBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        _db.TwoFactorPendingTokens.Add(
            new TwoFactorPendingToken
            {
                TokenHash = HashToken(rawToken),
                UserId = userId,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
            }
        );

        await _db.SaveChangesAsync();
        return rawToken;
    }

    /// <summary>
    /// Xác minh pending token hợp lệ và đánh dấu đã dùng (consume một lần).
    /// Trả về userId nếu hợp lệ, null nếu không hợp lệ / hết hạn / đã dùng.
    /// </summary>
    public async Task<string?> ConsumePendingTokenAsync(string rawToken)
    {
        var hash = HashToken(rawToken);

        var token = await _db
            .TwoFactorPendingTokens.Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash);

        if (token == null || !token.IsActive || !token.User.IsActive)
            return null;

        token.UsedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        return token.UserId;
    }
}
