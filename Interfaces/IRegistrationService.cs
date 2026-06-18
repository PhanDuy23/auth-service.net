using auth_service.DTOs.Requests;
using auth_service.Response;

namespace auth_service.Interfaces;

public interface IRegistrationService
{
    /// <summary>
    /// Tạo tài khoản mới và gửi email xác thực.
    /// </summary>
    Task<ApiResponse<object>> RegisterAsync(RegisterDto dto);

    /// <summary>
    /// Xác nhận email bằng token từ link trong email.
    /// </summary>
    Task<ApiResponse<object>> ConfirmEmailAsync(string userId, string token);
}
