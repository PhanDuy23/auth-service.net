using System.Security.Claims;
using auth_service.DTOs.Requests;
using auth_service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace auth_service.Controllers;

[ApiController]
[Route("api/auth/2fa")]
public class TwoFactorController : ControllerBase
{
    private readonly ITwoFactorService _twoFactorService;
    private readonly ICookieService _cookieService;

    public TwoFactorController(ITwoFactorService twoFactorService, ICookieService cookieService)
    {
        _twoFactorService = twoFactorService;
        _cookieService = cookieService;
    }

    /// <summary>
    /// Bước 1: Tạo secret key và QR code URI để cài vào authenticator app.
    /// Frontend dùng <c>qrCodeUri</c> để render QR code (ví dụ: qrcode.js).
    /// Sau khi quét, gọi <c>POST /api/auth/2fa/enable</c> để bật 2FA.
    /// </summary>
    [Authorize]
    [HttpGet("setup")]
    public async Task<IActionResult> GetSetup()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(UnauthorizedBody());

        var result = await _twoFactorService.GenerateSetupAsync(userId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Bước 2: Xác nhận mã TOTP từ authenticator app và bật 2FA.
    /// Body yêu cầu <c>secretKey</c> (từ /2fa/setup) và <c>code</c> (mã 6 chữ số).
    /// Response trả về <c>recoveryCodes</c> — lưu lại ngay, chỉ hiển thị một lần.
    /// </summary>
    [Authorize]
    [HttpPost("enable")]
    public async Task<IActionResult> Enable([FromBody] EnableTwoFactorDto dto)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(UnauthorizedBody());

        var result = await _twoFactorService.EnableAsync(userId, dto.SecretKey, dto.Code);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Tắt 2FA. Yêu cầu xác minh mã TOTP hiện tại. Xóa tất cả recovery codes.
    /// </summary>
    [Authorize]
    [HttpPost("disable")]
    public async Task<IActionResult> Disable([FromBody] TwoFactorToggleDto dto)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(UnauthorizedBody());

        if (string.IsNullOrEmpty(dto.Code))
            return BadRequest(
                new
                {
                    success = false,
                    message = "Mã xác thực là bắt buộc khi tắt 2FA",
                    statusCode = 400,
                }
            );

        var result = await _twoFactorService.DisableAsync(userId, dto.Code);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Xác thực mã TOTP sau bước login.
    /// Chỉ gọi khi <c>POST /login</c> trả về <c>requiresTwoFactor: true</c>.
    /// Trả về access token và set refresh token cookie nếu mã hợp lệ.
    /// </summary>
    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] TwoFactorVerifyDto dto)
    {
        var pendingToken = _cookieService.GetPendingToken();
        if (string.IsNullOrEmpty(pendingToken))
            return Unauthorized(
                new
                {
                    success = false,
                    message = "Phiên xác thực không tìm thấy. Vui lòng đăng nhập lại.",
                    statusCode = 401,
                }
            );

        var (result, rawRefreshToken, expiresAt) = await _twoFactorService.VerifyLoginAsync(
            pendingToken,
            dto
        );

        // Xóa pending token cookie dù thành công hay thất bại (đã consumed hoặc hết hạn)
        _cookieService.ClearPendingTokenCookie();

        if (result.Success && rawRefreshToken is not null && expiresAt is not null)
            _cookieService.SetRefreshTokenCookie(rawRefreshToken, expiresAt.Value, dto.RememberMe);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("verify-recovery")]
    public async Task<IActionResult> VerifyRecovery([FromBody] RecoveryCodeVerifyDto dto)
    {
        var pendingToken = _cookieService.GetPendingToken();
        if (string.IsNullOrEmpty(pendingToken))
            return Unauthorized(
                new
                {
                    success = false,
                    message = "Phiên xác thực không tìm thấy. Vui lòng đăng nhập lại.",
                    statusCode = 401,
                }
            );

        var (result, rawRefreshToken, expiresAt) = await _twoFactorService.VerifyRecoveryCodeAsync(
            pendingToken,
            dto
        );

        _cookieService.ClearPendingTokenCookie();

        if (result.Success && rawRefreshToken is not null && expiresAt is not null)
            _cookieService.SetRefreshTokenCookie(rawRefreshToken, expiresAt.Value, dto.RememberMe);

        return StatusCode(result.StatusCode, result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string? GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

    private static object UnauthorizedBody() =>
        new
        {
            success = false,
            message = "Không xác định được người dùng",
            statusCode = 401,
        };
}
