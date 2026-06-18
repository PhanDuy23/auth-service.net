using auth_service.DTOs.Requests;
using auth_service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace auth_service.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICookieService _cookieService;

    public AuthController(IAuthService authService, ICookieService cookieService)
    {
        _authService = authService;
        _cookieService = cookieService;
    }

    /// <summary>
    /// Đăng nhập bằng email và mật khẩu.
    /// Nếu tài khoản đã bật 2FA, response trả về <c>requiresTwoFactor: true</c> —
    /// client cần gọi <c>POST /api/auth/2fa/verify</c>.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var (result, rawRefreshToken, expiresAt, pendingToken) = await _authService.LoginAsync(dto);

        if (result.Success && rawRefreshToken is not null && expiresAt is not null)
        {
            // Login hoàn tất ngay (không có 2FA)
            _cookieService.SetRefreshTokenCookie(rawRefreshToken, expiresAt.Value, dto.RememberMe);
        }
        else if (
            result.Success
            && result.Data?.RequiresTwoFactor == true
            && pendingToken is not null
        )
        {
            // Bước 1 của 2FA — set pending token vào HttpOnly cookie
            _cookieService.SetPendingTokenCookie(pendingToken);
        }

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Cấp lại access token bằng refresh token cookie.
    /// </summary>
    [HttpPost("refresh-access-token")]
    public async Task<IActionResult> Refresh()
    {
        var rawToken = _cookieService.GetRefreshToken();
        if (string.IsNullOrEmpty(rawToken))
            return Unauthorized(
                new
                {
                    success = false,
                    message = "Refresh token không tìm thấy",
                    statusCode = 401,
                }
            );

        var result = await _authService.RefreshAccessTokenAsync(rawToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Đăng xuất — thu hồi refresh token và xóa cookie.
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var rawToken = _cookieService.GetRefreshToken();

        if (!string.IsNullOrEmpty(rawToken))
            await _authService.RevokeAsync(rawToken);

        _cookieService.ClearRefreshTokenCookie();

        return Ok(
            new
            {
                success = true,
                message = "Đăng xuất thành công",
                statusCode = 200,
            }
        );
    }
}
