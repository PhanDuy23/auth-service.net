using System.Security.Claims;
using auth_service.DTOs.Requests;
using auth_service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace auth_service.Controllers;

[ApiController]
[Route("api/auth")]
public class PasswordController : ControllerBase
{
    private readonly IPasswordService _passwordService;

    public PasswordController(IPasswordService passwordService)
    {
        _passwordService = passwordService;
    }

    /// <summary>
    /// Gửi link reset mật khẩu qua email.
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var result = await _passwordService.ForgotPasswordAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Đặt lại mật khẩu bằng token từ email.
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var result = await _passwordService.ResetPasswordAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Đổi mật khẩu khi đã đăng nhập (yêu cầu xác thực).
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(
                new
                {
                    success = false,
                    message = "Không xác định được người dùng",
                    statusCode = 401,
                }
            );

        var result = await _passwordService.ChangePasswordAsync(userId, dto);
        return StatusCode(result.StatusCode, result);
    }
}
