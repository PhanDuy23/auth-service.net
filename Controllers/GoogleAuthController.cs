using auth_service.Interfaces;
using auth_service.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace auth_service.Controllers;

[ApiController]
[Route("api/auth/google")]
public class GoogleAuthController : ControllerBase
{
    private readonly IGoogleAuthService _googleAuthService;
    private readonly AppSettings _appSettings;
    private readonly ICookieService _cookieService;

    public GoogleAuthController(
        IGoogleAuthService googleAuthService,
        IOptions<AppSettings> appSettings,
        ICookieService cookieService
    )
    {
        _googleAuthService = googleAuthService;
        _appSettings = appSettings.Value;
        _cookieService = cookieService;
    }

    /// <summary>
    /// Bắt đầu flow đăng nhập Google — redirect sang Google consent screen.
    /// </summary>
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? returnUrl = null)
    {
        var callbackUrl = Url.Action(
            nameof(Callback),
            "GoogleAuth",
            new { returnUrl },
            "http" //request.Scheme là lấy theo request hiện tại
        );

        var properties = new AuthenticationProperties { RedirectUri = callbackUrl };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Callback được Google gọi sau khi user đồng ý cấp quyền.
    /// Server xử lý, tạo JWT + set cookie, sau đó redirect về frontend.
    /// </summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string? returnUrl = null)
    {
        var (result, rawRefreshToken, expiresAt) = await _googleAuthService.HandleCallbackAsync(
            returnUrl
        );

        if (!result.Success)
        {
            var errorUrl =
                $"{_appSettings.FrontendUrl}/html/login.html?error={Uri.EscapeDataString(result.Message)}";
            return Redirect(errorUrl);
        }

        if (result.Success && rawRefreshToken is not null && expiresAt is not null)
        {
            // Login hoàn tất ngay (không có 2FA), không persistent
            _cookieService.SetRefreshTokenCookie(rawRefreshToken, expiresAt.Value, false);
        }

        var destination = string.IsNullOrEmpty(returnUrl)
            ? $"{_appSettings.FrontendUrl}/html/home.html"
            : returnUrl;

        return Redirect(destination);
    }
}
