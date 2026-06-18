using auth_service.DTOs.Requests;
using auth_service.DTOs.Responses;
using auth_service.Interfaces;
using auth_service.Models;
using auth_service.Response;
using auth_service.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using OtpNet;

namespace auth_service.Services;

public class TwoFactorService : ITwoFactorService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TokenService _tokenService;
    private readonly IRecoveryCodeService _recoveryCodeService;
    private readonly AppSettings _appSettings;

    public TwoFactorService(
        UserManager<ApplicationUser> userManager,
        TokenService tokenService,
        IRecoveryCodeService recoveryCodeService,
        IOptions<AppSettings> appSettings
    )
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _recoveryCodeService = recoveryCodeService;
        _appSettings = appSettings.Value;
    }

    public async Task<ApiResponse<TwoFactorSetupDto>> GenerateSetupAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
            return ApiResponse<TwoFactorSetupDto>.ErrorResponse("Người dùng không tồn tại", 404);

        if (user.TwoFactorEnabled)
            return ApiResponse<TwoFactorSetupDto>.ErrorResponse(
                "2FA đã được bật cho tài khoản này",
                400
            );

        // 20 bytes (160-bit) — secret tạm, chưa lưu DB
        var secretBytes = KeyGeneration.GenerateRandomKey(20);
        var secretBase32 = Base32Encoding.ToString(secretBytes);

        var issuer = Uri.EscapeDataString(_appSettings.AppName ?? "AuthService");
        var account = Uri.EscapeDataString(user.Email!);
        var qrUri =
            $"otpauth://totp/{issuer}:{account}?secret={secretBase32}&issuer={issuer}&algorithm=SHA1&digits=6&period=30";

        return ApiResponse<TwoFactorSetupDto>.SuccessResponse(
            new TwoFactorSetupDto { SecretKey = secretBase32, QrCodeUri = qrUri },
            "Quét QR code hoặc nhập secret key vào ứng dụng authenticator, sau đó xác nhận bằng endpoint /api/auth/2fa/enable"
        );
    }

    public async Task<ApiResponse<RecoveryCodesDto>> EnableAsync(
        string userId,
        string secretKey,
        string code
    )
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
            return ApiResponse<RecoveryCodesDto>.ErrorResponse("Người dùng không tồn tại", 404);

        if (user.TwoFactorEnabled)
            return ApiResponse<RecoveryCodesDto>.ErrorResponse(
                "2FA đã được bật cho tài khoản này",
                400
            );

        if (!VerifyTotpCode(secretKey, code))
            return ApiResponse<RecoveryCodesDto>.ErrorResponse(
                "Mã xác thực không hợp lệ hoặc đã hết hạn",
                400
            );

        user.TwoFactorSecretKey = secretKey;
        user.TwoFactorEnabled = true;
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            return ApiResponse<RecoveryCodesDto>.ErrorResponse(
                "Bật 2FA thất bại",
                500,
                result.Errors.Select(e => e.Description).ToList()
            );

        // Sinh recovery codes (8 codes mặc định)
        var recoveryCodes = await _recoveryCodeService.GenerateCodesAsync(userId, count: 8);

        return ApiResponse<RecoveryCodesDto>.SuccessResponse(
            recoveryCodes,
            "Bật 2FA thành công. Vui lòng lưu lại các recovery codes — chúng chỉ hiển thị một lần duy nhất."
        );
    }

    public async Task<ApiResponse<object>> DisableAsync(string userId, string code)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
            return ApiResponse<object>.ErrorResponse("Người dùng không tồn tại", 404);

        if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecretKey))
            return ApiResponse<object>.ErrorResponse("2FA chưa được bật cho tài khoản này", 400);

        if (!VerifyTotpCode(user.TwoFactorSecretKey, code))
            return ApiResponse<object>.ErrorResponse(
                "Mã xác thực không hợp lệ hoặc đã hết hạn",
                400
            );

        user.TwoFactorSecretKey = null;
        user.TwoFactorEnabled = false;
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            return ApiResponse<object>.ErrorResponse(
                "Tắt 2FA thất bại",
                500,
                result.Errors.Select(e => e.Description).ToList()
            );

        // Xóa tất cả recovery codes
        await _recoveryCodeService.RevokeAllAsync(userId);

        return ApiResponse<object>.SuccessResponse(null, "Tắt 2FA thành công");
    }

    public async Task<(
        ApiResponse<AuthResponseDto> response,
        string? rawRefreshToken,
        DateTimeOffset? refreshTokenExpiresAt
    )> VerifyLoginAsync(string pendingToken, TwoFactorVerifyDto dto)
    {
        var userId = await _tokenService.ConsumePendingTokenAsync(pendingToken);
        if (userId == null)
        {
            return (
                ApiResponse<AuthResponseDto>.ErrorResponse(
                    "Phiên xác thực không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại.",
                    401
                ),
                null,
                null
            );
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            return (
                ApiResponse<AuthResponseDto>.ErrorResponse("Người dùng không tồn tại", 401),
                null,
                null
            );
        }

        if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecretKey))
        {
            return (
                ApiResponse<AuthResponseDto>.ErrorResponse("Tài khoản chưa bật 2FA", 400),
                null,
                null
            );
        }

        if (!VerifyTotpCode(user.TwoFactorSecretKey, dto.Code))
        {
            return (
                ApiResponse<AuthResponseDto>.ErrorResponse(
                    "Mã xác thực không hợp lệ hoặc đã hết hạn",
                    400
                ),
                null,
                null
            );
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, accessExpiresAt) = _tokenService.GenerateJwtToken(user, roles);
        var (rawRefreshToken, refreshToken) = await _tokenService.CreateRefreshTokenAsync(user.Id);

        var response = new AuthResponseDto
        {
            AccessToken = accessToken,
            ExpiresAt = accessExpiresAt,
            Email = user.Email!,
            FullName = user.FullName ?? string.Empty,
            Roles = roles,
        };

        return (
            ApiResponse<AuthResponseDto>.SuccessResponse(response, "Đăng nhập thành công"),
            rawRefreshToken,
            refreshToken.ExpiresAt
        );
    }

    // ── Recovery Code Authentication ──────────────────────────────────────────

    public async Task<(
        ApiResponse<AuthResponseDto> response,
        string? rawRefreshToken,
        DateTimeOffset? refreshTokenExpiresAt
    )> VerifyRecoveryCodeAsync(string pendingToken, RecoveryCodeVerifyDto dto)
    {
        var userId = await _tokenService.ConsumePendingTokenAsync(pendingToken);
        if (userId == null)
        {
            return (
                ApiResponse<AuthResponseDto>.ErrorResponse(
                    "Phiên xác thực không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại.",
                    401
                ),
                null,
                null
            );
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            return (
                ApiResponse<AuthResponseDto>.ErrorResponse("Người dùng không tồn tại", 401),
                null,
                null
            );
        }

        if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecretKey))
        {
            return (
                ApiResponse<AuthResponseDto>.ErrorResponse("Tài khoản chưa bật 2FA", 400),
                null,
                null
            );
        }

        // Xác minh và đánh dấu code đã dùng
        var isValid = await _recoveryCodeService.VerifyAndMarkUsedAsync(user.Id, dto.RecoveryCode);

        if (!isValid)
        {
            return (
                ApiResponse<AuthResponseDto>.ErrorResponse(
                    "Recovery code không hợp lệ hoặc đã được sử dụng",
                    400
                ),
                null,
                null
            );
        }

        // ⚠️ TẮT 2FA SAU KHI DÙNG RECOVERY CODE — yêu cầu người dùng cấu hình lại
        user.TwoFactorSecretKey = null;
        user.TwoFactorEnabled = false;
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Xóa tất cả recovery codes còn lại
        await _recoveryCodeService.RevokeAllAsync(user.Id);

        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, accessExpiresAt) = _tokenService.GenerateJwtToken(user, roles);
        var (rawRefreshToken, refreshToken) = await _tokenService.CreateRefreshTokenAsync(user.Id);

        var response = new AuthResponseDto
        {
            AccessToken = accessToken,
            ExpiresAt = accessExpiresAt,
            Email = user.Email!,
            FullName = user.FullName ?? string.Empty,
            Roles = roles,
            RequiresTwoFactorReconfiguration = true, // ← cờ đặc biệt báo frontend
        };

        return (
            ApiResponse<AuthResponseDto>.SuccessResponse(
                response,
                "Đăng nhập thành công bằng recovery code. 2FA đã bị tắt — vui lòng cấu hình lại để đảm bảo an toàn."
            ),
            rawRefreshToken,
            refreshToken.ExpiresAt
        );
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Xác minh mã TOTP 6 chữ số. Cho phép window ±1 step (30s) để tránh clock skew.
    /// </summary>
    private static bool VerifyTotpCode(string secretBase32, string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6)
            return false;

        try
        {
            var secretBytes = Base32Encoding.ToBytes(secretBase32);
            var totp = new Totp(secretBytes);
            return totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
        }
        catch
        {
            return false;
        }
    }
}
