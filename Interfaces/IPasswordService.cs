using auth_service.DTOs.Requests;
using auth_service.Response;

namespace auth_service.Interfaces;

public interface IPasswordService
{
    /// <summary>
    /// Gửi link reset mật khẩu qua email. Không tiết lộ user có tồn tại hay không.
    /// </summary>
    Task<ApiResponse<object>> ForgotPasswordAsync(ForgotPasswordDto dto);

    /// <summary>
    /// Đặt lại mật khẩu mới bằng token từ email.
    /// </summary>
    Task<ApiResponse<object>> ResetPasswordAsync(ResetPasswordDto dto);

    /// <summary>
    /// Đổi mật khẩu khi đã đăng nhập (yêu cầu mật khẩu hiện tại).
    /// </summary>
    Task<ApiResponse<object>> ChangePasswordAsync(string userId, ChangePasswordDto dto);
}
