using auth_service.Data;
using auth_service.DTOs.Requests;
using auth_service.DTOs.Responses;
using auth_service.Interfaces;
using auth_service.Models;
using auth_service.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace auth_service.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly TokenService _tokenService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        TokenService tokenService
    )
    {
        _userManager = userManager;
        _db = db;
        _tokenService = tokenService;
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    public async Task<(
        ApiResponse<AuthResponseDto> response,
        string? rawRefreshToken,
        DateTimeOffset? refreshTokenExpiresAt,
        string? pendingToken
    )> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || !user.IsActive)
        {
            return (
                ApiResponse<AuthResponseDto>.ErrorResponse("Email hoặc mật khẩu không đúng", 401),
                null,
                null,
                null
            );
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            var remaining = lockoutEnd.HasValue
                ? (int)Math.Ceiling((lockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes)
                : 0;

            return (
                ApiResponse<AuthResponseDto>.ErrorResponse(
                    $"Tài khoản tạm thời bị khóa do đăng nhập sai quá nhiều lần. Vui lòng thử lại sau {remaining} phút.",
                    423
                ),
                null,
                null,
                null
            );
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!isPasswordValid)
        {
            await _userManager.AccessFailedAsync(user);

            if (await _userManager.IsLockedOutAsync(user))
            {
                return (
                    ApiResponse<AuthResponseDto>.ErrorResponse(
                        "Tài khoản đã bị khóa tạm thời do đăng nhập sai quá nhiều lần.",
                        423
                    ),
                    null,
                    null,
                    null
                );
            }

            return (
                ApiResponse<AuthResponseDto>.ErrorResponse("Email hoặc mật khẩu không đúng", 401),
                null,
                null,
                null
            );
        }

        if (!await _userManager.IsEmailConfirmedAsync(user))
        {
            return (
                ApiResponse<AuthResponseDto>.ErrorResponse("Tài khoản chưa xác thực email", 400),
                null,
                null,
                null
            );
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        // Nếu đã bật 2FA → phát pending token, client gọi tiếp POST /api/auth/2fa/verify
        if (user.TwoFactorEnabled && !string.IsNullOrEmpty(user.TwoFactorSecretKey))
        {
            var pendingToken = await _tokenService.CreatePendingTokenAsync(user.Id);

            var pending = new AuthResponseDto { RequiresTwoFactor = true, Email = user.Email! };

            return (
                ApiResponse<AuthResponseDto>.SuccessResponse(
                    pending,
                    "Vui lòng nhập mã xác thực 2FA",
                    200
                ),
                null,
                null,
                pendingToken
            );
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, accessExpiresAt) = _tokenService.GenerateJwtToken(user, roles);
        var (rawRefreshToken, refreshToken) = await _tokenService.CreateRefreshTokenAsync(user.Id);

        var response = new AuthResponseDto
        {
            AccessToken = accessToken,
            ExpiresAt = accessExpiresAt,
            Email = user.Email!,
            FullName = user.FullName ?? string.Empty,
            Roles = roles,
        };

        return (
            ApiResponse<AuthResponseDto>.SuccessResponse(response, "Đăng nhập thành công"),
            rawRefreshToken,
            refreshToken.ExpiresAt,
            null
        );
    }

    // ── Refresh Access Token ──────────────────────────────────────────────────

    public async Task<ApiResponse<AuthResponseDto>> RefreshAccessTokenAsync(string rawRefreshToken)
    {
        var tokenHash = TokenService.HashToken(rawRefreshToken);

        var stored = await _db
            .RefreshTokens.Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (stored == null || !stored.IsActive || !stored.User.IsActive)
        {
            return ApiResponse<AuthResponseDto>.ErrorResponse(
                "Refresh token không hợp lệ hoặc đã hết hạn",
                401
            );
        }

        var user = stored.User;
        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, accessExpiresAt) = _tokenService.GenerateJwtToken(user, roles);

        var response = new AuthResponseDto
        {
            AccessToken = accessToken,
            ExpiresAt = accessExpiresAt,
            Email = user.Email!,
            FullName = user.FullName ?? string.Empty,
            Roles = roles,
        };

        return ApiResponse<AuthResponseDto>.SuccessResponse(
            response,
            "Làm mới access token thành công"
        );
    }

    // ── Revoke ────────────────────────────────────────────────────────────────

    public async Task<ApiResponse<object>> RevokeAsync(string rawRefreshToken)
    {
        var tokenHash = TokenService.HashToken(rawRefreshToken);

        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (stored == null || stored.IsRevoked)
        {
            return ApiResponse<object>.ErrorResponse("Refresh token không hợp lệ", 400);
        }

        stored.RevokedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(null, "Đăng xuất thành công");
    }
}
