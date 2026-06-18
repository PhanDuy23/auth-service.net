using auth_service.DTOs.Requests;
using auth_service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace auth_service.Controllers;

[ApiController]
[Route("api/auth/github")]
public class GitHubAuthController : ControllerBase
{
    private readonly IGitHubAuthService _gitHubAuthService;
    private readonly ICookieService _cookieService;

    public GitHubAuthController(IGitHubAuthService gitHubAuthService, ICookieService cookieService)
    {
        _gitHubAuthService = gitHubAuthService;
        _cookieService = cookieService;
    }

    /// <summary>
    /// Frontend gửi authorization code nhận được từ GitHub OAuth callback.
    /// Backend đổi code → access token → lấy profile → kiểm tra / tạo / link user → cấp JWT.
    /// Refresh token được set qua HttpOnly cookie, access token trả về trong JSON body.
    /// </summary>
    [HttpPost("callback")]
    public async Task<IActionResult> Callback([FromBody] GitHubCallbackDto dto)
    {
        var (result, rawRefreshToken, expiresAt) = await _gitHubAuthService.HandleCallbackAsync(
            dto.Code
        );

        if (!result.Success)
            return StatusCode(result.StatusCode, result);

        // Set refresh token cookie (không persistent — GitHub login không có "remember me")
        if (rawRefreshToken is not null && expiresAt is not null)
            _cookieService.SetRefreshTokenCookie(rawRefreshToken, expiresAt.Value, false);

        return Ok(result);
    }
}
