using auth_service.DTOs.Requests;
using auth_service.DTOs.Responses;
using auth_service.Response;

namespace auth_service.Interfaces;

public interface ITwoFactorService
{
    /// <summary>
    /// Bước 1: Tạo secret key tạm và trả về URI để frontend render QR code.
    /// Secret chưa được lưu vào DB — chỉ lưu sau khi user xác nhận bằng <see cref="EnableAsync"/>.
    /// </summary>
    Task<ApiResponse<TwoFactorSetupDto>> GenerateSetupAsync(string userId);

    /// <summary>
    /// Bước 2: Xác minh mã TOTP và bật 2FA. Lưu secret key vào DB.
    /// Trả về recovery codes (raw plain-text) — đây là lần duy nhất hiển thị.
    /// </summary>
    Task<ApiResponse<RecoveryCodesDto>> EnableAsync(string userId, string secretKey, string code);

    /// <summary>
    /// Tắt 2FA sau khi xác minh mã TOTP hiện tại. Xóa tất cả recovery codes.
    /// </summary>
    Task<ApiResponse<object>> DisableAsync(string userId, string code);

    /// <summary>
    /// Xác thực mã TOTP trong luồng đăng nhập 2FA.
    /// pendingToken lấy từ cookie HttpOnly, chứng minh đã qua bước mật khẩu.
    /// </summary>
    Task<(
        ApiResponse<AuthResponseDto> response,
        string? rawRefreshToken,
        DateTimeOffset? refreshTokenExpiresAt
    )> VerifyLoginAsync(string pendingToken, TwoFactorVerifyDto dto);

    /// <summary>
    /// Xác thực bằng recovery code khi mất quyền truy cập authenticator.
    /// pendingToken lấy từ cookie HttpOnly, chứng minh đã qua bước mật khẩu.
    /// Sau khi dùng recovery code, tắt 2FA và yêu cầu người dùng cấu hình lại.
    /// </summary>
    Task<(
        ApiResponse<AuthResponseDto> response,
        string? rawRefreshToken,
        DateTimeOffset? refreshTokenExpiresAt
    )> VerifyRecoveryCodeAsync(string pendingToken, RecoveryCodeVerifyDto dto);
}
