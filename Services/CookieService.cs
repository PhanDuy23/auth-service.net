using auth_service.Interfaces;

namespace auth_service.Services;

public class CookieService : ICookieService
{
    private const string RefreshTokenCookie = "refreshToken";
    private const string PendingTokenCookie = "2fa_pending";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWebHostEnvironment _env;

    public CookieService(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env)
    {
        _httpContextAccessor = httpContextAccessor;
        _env = env;
    }

    private HttpResponse Response =>
        _httpContextAccessor.HttpContext?.Response
        ?? throw new InvalidOperationException("HttpContext không khả dụng.");

    private HttpRequest Request =>
        _httpContextAccessor.HttpContext?.Request
        ?? throw new InvalidOperationException("HttpContext không khả dụng.");

    // ── Refresh Token ──────────────────────────────────────────────────────────

    public void SetRefreshTokenCookie(string token, DateTimeOffset expiresAt, bool persistent)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(),
            SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.Strict,
        };

        if (persistent)
            options.Expires = expiresAt;

        Response.Cookies.Append(RefreshTokenCookie, token, options);
    }

    public void ClearRefreshTokenCookie()
    {
        Response.Cookies.Append(
            RefreshTokenCookie,
            string.Empty,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = !_env.IsDevelopment(),
                SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(-1),
            }
        );
    }

    public string? GetRefreshToken() => Request.Cookies[RefreshTokenCookie];

    // ── 2FA Pending Token ──────────────────────────────────────────────────────

    public void SetPendingTokenCookie(string token)
    {
        // Session cookie (không có Expires) — tự mất khi đóng browser
        // TTL thực tế do DB kiểm soát (5 phút)
        Response.Cookies.Append(
            PendingTokenCookie,
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = !_env.IsDevelopment(),
                SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.Strict,
                Path = "/api/auth/2fa", // chỉ gửi đến các endpoint /2fa/*
            }
        );
    }

    public void ClearPendingTokenCookie()
    {
        Response.Cookies.Append(
            PendingTokenCookie,
            string.Empty,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = !_env.IsDevelopment(),
                SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.Strict,
                Path = "/api/auth/2fa",
                Expires = DateTimeOffset.UtcNow.AddDays(-1),
            }
        );
    }

    public string? GetPendingToken() => Request.Cookies[PendingTokenCookie];
}
