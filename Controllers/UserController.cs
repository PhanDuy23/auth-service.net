using System.Security.Claims;
using auth_service.Authorization;
using auth_service.DTOs.Requests;
using auth_service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace auth_service.Controllers;

[ApiController]
[Route("api")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuthorizationService _authorizationService;

    public UserController(IUserService userService, IAuthorizationService authorizationService)
    {
        _userService = userService;
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Lấy profile của chính mình
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
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

        var result = await _userService.GetProfileAsync(userId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Cập nhật profile — chỉ được sửa profile của chính mình
    /// </summary>
    [Authorize]
    [HttpPut("users/{id}/profile")]
    public async Task<IActionResult> UpdateProfile(string id, [FromBody] UpdateProfileDto dto)
    {
        var authResult = await _authorizationService.AuthorizeAsync(
            User,
            id,
            Permissions.ProfileEdit
        );
        if (!authResult.Succeeded)
            return StatusCode(
                403,
                new
                {
                    success = false,
                    message = "Bạn không có quyền sửa profile của người khác",
                    statusCode = 403,
                }
            );

        var result = await _userService.UpdateProfileAsync(id, dto);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Lấy danh sách tất cả user — Permissions.UsersRead  có quyền truy cập (Admin và Employee)
    /// </summary>
    [Authorize(Policy = Permissions.UsersRead)]
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var result = await _userService.GetUsersAsync();
        return StatusCode(result.StatusCode, result);
    }
}
