using auth_service.DTOs.Responses;
using auth_service.Response;

namespace auth_service.Interfaces;

public interface IGitHubAuthService
{
    /// <summary>
    /// Đổi authorization code (từ GitHub OAuth callback) lấy access token,
    /// lấy thông tin user, sau đó kiểm tra / tạo / link tài khoản và cấp JWT.
    /// </summary>
    Task<(
        ApiResponse<AuthResponseDto> response,
        string? rawRefreshToken,
        DateTimeOffset? refreshTokenExpiresAt
    )> HandleCallbackAsync(string code);
}
