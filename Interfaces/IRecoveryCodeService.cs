using auth_service.DTOs.Responses;

namespace auth_service.Interfaces;

/// <summary>
/// Quản lý Recovery Codes — codes dự phòng khi người dùng mất quyền truy cập authenticator app.
/// </summary>
public interface IRecoveryCodeService
{
    /// <summary>
    /// Sinh 8–10 recovery codes mới cho user (khi bật 2FA hoặc regenerate).
    /// Trả về raw codes — server chỉ lưu hash.
    /// </summary>
    Task<RecoveryCodesDto> GenerateCodesAsync(string userId, int count = 8);

    /// <summary>
    /// Xác minh recovery code. Nếu hợp lệ, đánh dấu code đã dùng.
    /// Trả về true nếu code hợp lệ và chưa dùng.
    /// </summary>
    Task<bool> VerifyAndMarkUsedAsync(string userId, string rawCode);

    /// <summary>
    /// Xóa tất cả recovery codes của user (khi tắt 2FA hoặc reset).
    /// </summary>
    Task RevokeAllAsync(string userId);
}
