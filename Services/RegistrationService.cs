using auth_service.DTOs.Requests;
using auth_service.Interfaces;
using auth_service.Models;
using auth_service.Response;
using auth_service.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace auth_service.Services;

public class RegistrationService : IRegistrationService
{
    private static readonly HashSet<string> AllowedRoles = ["Customer", "Employee", "Admin"];

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly AppSettings _appSettings;

    public RegistrationService(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IOptions<AppSettings> appSettings
    )
    {
        _userManager = userManager;
        _emailService = emailService;
        _appSettings = appSettings.Value;
    }

    public async Task<ApiResponse<object>> RegisterAsync(RegisterDto dto)
    {
        var role = dto.Role;
        if (!AllowedRoles.Contains(role))
        {
            return ApiResponse<object>.ErrorResponse(
                "Đăng ký thất bại",
                400,
                [$"Role không hợp lệ. Các role được phép: {string.Join(", ", AllowedRoles)}"]
            );
        }

        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            return ApiResponse<object>.ErrorResponse(
                "Đăng ký thất bại",
                400,
                ["Email đã được sử dụng"]
            );
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
        };

        var createResult = await _userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
        {
            return ApiResponse<object>.ErrorResponse(
                "Đăng ký thất bại",
                400,
                createResult.Errors.Select(e => e.Description).ToList()
            );
        }

        var addRoleResult = await _userManager.AddToRoleAsync(user, role);
        if (!addRoleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return ApiResponse<object>.ErrorResponse(
                "Đăng ký thất bại",
                500,
                addRoleResult.Errors.Select(e => e.Description).ToList()
            );
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);

        // Frontend nhận link này và gọi POST /api/auth/confirm-email
        var confirmUrl =
            $"{_appSettings.FrontendUrl}/confirm-email"
            + $"?userId={user.Id}"
            + $"&token={encodedToken}";

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _emailService.SendEmailAsync(
                user.Email!,
                "Confirm your email",
                $"""
                <h2>Xác thực tài khoản</h2>
                <a href="{confirmUrl}">Confirm Email</a>
                """,
                cts.Token
            );
        }
        catch (Exception ex)
        {
            await _userManager.DeleteAsync(user);
            return ApiResponse<object>.ErrorResponse(
                $"Gửi email xác thực thất bại: {ex.Message}",
                500
            );
        }

        return ApiResponse<object>.SuccessResponse(
            null,
            "Đăng ký thành công. Hãy xác nhận trong email của bạn",
            201
        );
    }

    public async Task<ApiResponse<object>> ConfirmEmailAsync(string userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return ApiResponse<object>.ErrorResponse("Người dùng không tồn tại", 400);
        }

        var decodedToken = Uri.UnescapeDataString(token);
        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

        if (!result.Succeeded)
        {
            return ApiResponse<object>.ErrorResponse(
                "Token xác thực không hợp lệ hoặc đã hết hạn",
                400
            );
        }

        return ApiResponse<object>.SuccessResponse(null, "Xác thực email thành công", 200);
    }
}
