using System.Security.Claims;
using auth_service.DTOs.Responses;
using auth_service.Interfaces;
using auth_service.Models;
using auth_service.Response;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;

namespace auth_service.Services;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TokenService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GoogleAuthService(
        UserManager<ApplicationUser> userManager,
        TokenService tokenService,
        IHttpContextAccessor httpContextAccessor
    )
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<(
        ApiResponse<AuthResponseDto> response,
        string? rawRefreshToken,
        DateTimeOffset? refreshTokenExpiresAt
    )> HandleCallbackAsync(string? returnUrl = null)
    {
        var httpContext =
            _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext không khả dụng.");

        // Xác thực callback từ Google — sau khi middleware xử lý, principal được lưu ở ExternalScheme
        var authResult = await httpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
        if (!authResult.Succeeded || authResult.Principal == null)
        {
            return (
                ApiResponse<AuthResponseDto>.ErrorResponse("Xác thực Google thất bại", 401),
                null,
                null
            );
        }

        var principal = authResult.Principal;

        // Lấy thông tin từ Google claims
        var googleId =
            principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Không tìm thấy Google ID trong token.");

        var email = principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
        {
            return (
                ApiResponse<AuthResponseDto>.ErrorResponse(
                    "Google không cung cấp địa chỉ email",
                    400
                ),
                null,
                null
            );
        }

        var firstName = principal.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty;
        var lastName = principal.FindFirstValue(ClaimTypes.Surname) ?? string.Empty;
        var fullName = $"{firstName} {lastName}".Trim();
        if (string.IsNullOrEmpty(fullName))
            fullName = principal.FindFirstValue(ClaimTypes.Name) ?? email;

        var loginInfo = new UserLoginInfo(GoogleDefaults.AuthenticationScheme, googleId, "Google");

        // Tìm user đã link Google trước đó
        var user = await _userManager.FindByLoginAsync(
            loginInfo.LoginProvider,
            loginInfo.ProviderKey
        );

        if (user == null)
        {
            // Chưa có link Google — tìm theo email
            user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                // Tạo user mới hoàn toàn
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    EmailConfirmed = true, // Google đã xác thực email rồi
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    var errors = createResult.Errors.Select(e => e.Description).ToList();
                    return (
                        ApiResponse<AuthResponseDto>.ErrorResponse(
                            "Tạo tài khoản thất bại",
                            500,
                            errors
                        ),
                        null,
                        null
                    );
                }

                await _userManager.AddToRoleAsync(user, "User");
            }
            else if (!user.IsActive)
            {
                return (
                    ApiResponse<AuthResponseDto>.ErrorResponse("Tài khoản đã bị vô hiệu hóa", 403),
                    null,
                    null
                );
            }

            // Link Google login vào account hiện có (hoặc mới tạo)
            var addLoginResult = await _userManager.AddLoginAsync(user, loginInfo);
            if (!addLoginResult.Succeeded)
            {
                // Có thể đã link rồi — bỏ qua lỗi duplicate, tiếp tục
                var isDuplicate = addLoginResult.Errors.Any(e =>
                    e.Code == "LoginAlreadyAssociated"
                );
                if (!isDuplicate)
                {
                    var errors = addLoginResult.Errors.Select(e => e.Description).ToList();
                    return (
                        ApiResponse<AuthResponseDto>.ErrorResponse(
                            "Liên kết tài khoản Google thất bại",
                            500,
                            errors
                        ),
                        null,
                        null
                    );
                }
            }
        }
        else if (!user.IsActive)
        {
            return (
                ApiResponse<AuthResponseDto>.ErrorResponse("Tài khoản đã bị vô hiệu hóa", 403),
                null,
                null
            );
        }

        // Cập nhật LastLoginAt
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // RefreshToken — KHÔNG qua 2FA, k có accesstoken vì phải redirect
        var (rawRefreshToken, refreshToken) = await _tokenService.CreateRefreshTokenAsync(user.Id);

        return (
            ApiResponse<AuthResponseDto>.SuccessResponse(null, "Đăng nhập Google thành công"),
            rawRefreshToken,
            refreshToken.ExpiresAt
        );
    }
}
