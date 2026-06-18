using auth_service.DTOs.Responses;
using auth_service.Response;

namespace auth_service.Interfaces;

public interface IGoogleAuthService
{
    /// <summary>
    /// Xử lý callback từ Google sau khi user đồng ý cấp quyền.
    /// - Tìm user theo email, nếu chưa có thì tự động tạo mới (EmailConfirmed = true).
    /// - Nếu đã có account email/password cùng email thì link Google login vào đó.
    /// - Không yêu cầu 2FA.
    /// Trả về JWT + raw refresh token như login thường.
    /// </summary>
    Task<(
        ApiResponse<AuthResponseDto> response,
        string? rawRefreshToken,
        DateTimeOffset? refreshTokenExpiresAt
    )> HandleCallbackAsync(string? returnUrl = null);
}
