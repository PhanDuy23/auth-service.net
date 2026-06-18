using auth_service.DTOs.Requests;
using auth_service.Interfaces;
using auth_service.Models;
using auth_service.Response;
using auth_service.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace auth_service.Services;

public class PasswordService : IPasswordService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly AppSettings _appSettings;

    public PasswordService(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IOptions<AppSettings> appSettings
    )
    {
        _userManager = userManager;
        _emailService = emailService;
        _appSettings = appSettings.Value;
    }

    public async Task<ApiResponse<object>> ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);

        // Không tiết lộ user có tồn tại hay không (security best practice)
        if (user == null || !user.IsActive)
        {
            return ApiResponse<object>.SuccessResponse(
                null,
                "Nếu email tồn tại, link reset mật khẩu đã được gửi đến email của bạn",
                200
            );
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);

        var resetUrl =
            $"{_appSettings.BaseUrl}/api/auth/reset-password"
            + $"?userId={user.Id}"
            + $"&token={encodedToken}";

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await _emailService.SendEmailAsync(
                user.Email!,
                "Reset Your Password",
                $"""
                <h2>Reset mật khẩu</h2>
                <p>Bạn đã yêu cầu reset mật khẩu. Click vào link bên dưới để tiếp tục:</p>
                <a href="{resetUrl}">Reset Password</a>
                <p>Link này sẽ hết hạn sau 1 giờ.</p>
                <p>Nếu bạn không yêu cầu reset mật khẩu, vui lòng bỏ qua email này.</p>
                """,
                cts.Token
            );
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.ErrorResponse(
                $"Gửi email reset mật khẩu thất bại: {ex.Message}",
                500
            );
        }

        return ApiResponse<object>.SuccessResponse(
            null,
            "Nếu email tồn tại, link reset mật khẩu đã được gửi đến email của bạn",
            200
        );
    }

    public async Task<ApiResponse<object>> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(dto.UserId);
        if (user == null || !user.IsActive)
        {
            return ApiResponse<object>.ErrorResponse("Yêu cầu reset mật khẩu không hợp lệ", 400);
        }

        var decodedToken = Uri.UnescapeDataString(dto.Token);
        var result = await _userManager.ResetPasswordAsync(user, decodedToken, dto.NewPassword);

        if (!result.Succeeded)
        {
            return ApiResponse<object>.ErrorResponse(
                "Token reset mật khẩu không hợp lệ hoặc đã hết hạn",
                400,
                result.Errors.Select(e => e.Description).ToList()
            );
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        await _userManager.SetLockoutEndDateAsync(user, null);

        // Gửi email thông báo (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try
            {
                await _emailService.SendEmailAsync(
                    user.Email!,
                    "Password Changed Successfully",
                    $"""
                    <h2>Mật khẩu đã được thay đổi</h2>
                    <p>Mật khẩu của bạn đã được thay đổi thành công vào lúc {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC.</p>
                    <p>Nếu bạn không thực hiện thao tác này, vui lòng liên hệ với chúng tôi ngay lập tức.</p>
                    """,
                    CancellationToken.None
                );
            }
            catch
            { /* Không fail operation nếu email thông báo lỗi */
            }
        });

        return ApiResponse<object>.SuccessResponse(
            null,
            "Đổi mật khẩu thành công. Bạn có thể đăng nhập với mật khẩu mới",
            200
        );
    }

    public async Task<ApiResponse<object>> ChangePasswordAsync(string userId, ChangePasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            return ApiResponse<object>.ErrorResponse("Người dùng không tồn tại", 404);
        }

        var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(
            user,
            dto.CurrentPassword
        );
        if (!isCurrentPasswordValid)
        {
            return ApiResponse<object>.ErrorResponse("Mật khẩu hiện tại không đúng", 400);
        }

        if (dto.CurrentPassword == dto.NewPassword)
        {
            return ApiResponse<object>.ErrorResponse(
                "Mật khẩu mới không được trùng với mật khẩu hiện tại",
                400
            );
        }

        var result = await _userManager.ChangePasswordAsync(
            user,
            dto.CurrentPassword,
            dto.NewPassword
        );

        if (!result.Succeeded)
        {
            return ApiResponse<object>.ErrorResponse(
                "Đổi mật khẩu thất bại",
                400,
                result.Errors.Select(e => e.Description).ToList()
            );
        }

        // Gửi email xác nhận (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try
            {
                await _emailService.SendEmailAsync(
                    user.Email!,
                    "Mật khẩu đã được thay đổi",
                    $"""
                    <h2>Mật khẩu đã được thay đổi</h2>
                    <p>Mật khẩu tài khoản của bạn vừa được thay đổi thành công vào lúc {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC.</p>
                    <p>Nếu bạn không thực hiện thao tác này, vui lòng liên hệ với chúng tôi ngay lập tức hoặc reset mật khẩu tại đây:</p>
                    <a href="{_appSettings.FrontendUrl}/forgot-password">Reset mật khẩu</a>
                    """,
                    CancellationToken.None
                );
            }
            catch
            { /* Không fail operation nếu gửi email lỗi */
            }
        });

        return ApiResponse<object>.SuccessResponse(null, "Đổi mật khẩu thành công", 200);
    }
}
