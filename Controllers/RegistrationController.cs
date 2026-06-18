using auth_service.DTOs.Requests;
using auth_service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace auth_service.Controllers;

[ApiController]
[Route("api/auth")]
public class RegistrationController : ControllerBase
{
    private readonly IRegistrationService _registrationService;

    public RegistrationController(IRegistrationService registrationService)
    {
        _registrationService = registrationService;
    }

    /// <summary>
    /// Đăng ký tài khoản mới. Gửi email xác thực sau khi tạo thành công.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var result = await _registrationService.RegisterAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Xác nhận email bằng token từ link trong email.
    /// </summary>
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto dto)
    {
        var result = await _registrationService.ConfirmEmailAsync(dto.UserId, dto.Token);
        return StatusCode(result.StatusCode, result);
    }
}
