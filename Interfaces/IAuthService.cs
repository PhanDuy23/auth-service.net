using auth_service.DTOs.Requests;
using auth_service.DTOs.Responses;
using auth_service.Response;

namespace auth_service.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Xác thực email + mật khẩu. Nếu tài khoản đã bật 2FA, trả về
    /// <c>requiresTwoFactor: true</c> — client cần gọi tiếp <c>POST /api/auth/2fa/verify</c>.
    /// </summary>
    Task<(
        ApiResponse<AuthResponseDto> response,
        string? rawRefreshToken,
        DateTimeOffset? refreshTokenExpiresAt,
        string? pendingToken
    )> LoginAsync(LoginDto dto);

    /// <summary>
    /// Cấp access token mới bằng refresh token hợp lệ.
    /// </summary>
    Task<ApiResponse<AuthResponseDto>> RefreshAccessTokenAsync(string rawRefreshToken);

    /// <summary>
    /// Thu hồi refresh token (đăng xuất).
    /// </summary>
    Task<ApiResponse<object>> RevokeAsync(string rawRefreshToken);
}
