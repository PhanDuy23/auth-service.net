using System.Net.Http.Headers;
using System.Text.Json;
using auth_service.DTOs.Responses;
using auth_service.Interfaces;
using auth_service.Models;
using auth_service.Response;
using auth_service.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace auth_service.Services;

public class GitHubAuthService : IGitHubAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TokenService _tokenService;
    private readonly GitHubOAuthSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;

    // Provider key dùng để nhận diện login provider trong AspNetUserLogins
    private const string ProviderName = "GitHub";

    public GitHubAuthService(
        UserManager<ApplicationUser> userManager,
        TokenService tokenService,
        IOptions<GitHubOAuthSettings> settings,
        IHttpClientFactory httpClientFactory
    )
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<(
        ApiResponse<AuthResponseDto> response,
        string? rawRefreshToken,
        DateTimeOffset? refreshTokenExpiresAt
    )> HandleCallbackAsync(string code)
    {
        // ── Bước 1: Đổi code lấy GitHub access token ─────────────────────────
        var githubToken = await ExchangeCodeForTokenAsync(code);
        if (githubToken is null)
        {
            return (
                ApiResponse<AuthResponseDto>.ErrorResponse(
                    "Không thể xác thực với GitHub. Code không hợp lệ hoặc đã hết hạn.",
                    401
                ),
                null,
                null
            );
        }

        // ── Bước 2: Lấy thông tin user từ GitHub API ─────────────────────────
        var githubUser = await GetGitHubUserAsync(githubToken);
        if (githubUser is null)
        {
            return (
                ApiResponse<AuthResponseDto>.ErrorResponse(
                    "Không thể lấy thông tin tài khoản GitHub.",
                    502
                ),
                null,
                null
            );
        }

        // GitHub cho phép ẩn email — cần gọi thêm /user/emails
        var email = githubUser.Email;
        if (string.IsNullOrEmpty(email))
        {
            email = await GetPrimaryEmailAsync(githubToken);
        }

        if (string.IsNullOrEmpty(email))
        {
            return (
                ApiResponse<AuthResponseDto>.ErrorResponse(
                    "Tài khoản GitHub không có email công khai. Vui lòng cài đặt email public trong GitHub settings.",
                    400
                ),
                null,
                null
            );
        }

        var githubId = githubUser.Id.ToString();
        var displayName = githubUser.Name ?? githubUser.Login ?? email;

        // ── Bước 3: Tìm / tạo / link user ────────────────────────────────────
        var loginInfo = new UserLoginInfo(ProviderName, githubId, ProviderName);

        // Tìm user đã link GitHub trước đó
        var user = await _userManager.FindByLoginAsync(ProviderName, githubId);

        if (user is null)
        {
            // Chưa link GitHub — tìm theo email
            user = await _userManager.FindByEmailAsync(email);

            if (user is null)
            {
                // Tạo tài khoản mới
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = displayName,
                    EmailConfirmed = true, // GitHub đã xác thực email
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    var errors = createResult.Errors.Select(e => e.Description).ToList();
                    return (
                        ApiResponse<AuthResponseDto>.ErrorResponse(
                            "Tạo tài khoản thất bại.",
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
                    ApiResponse<AuthResponseDto>.ErrorResponse("Tài khoản đã bị vô hiệu hóa.", 403),
                    null,
                    null
                );
            }

            // Link GitHub login vào account
            var addLoginResult = await _userManager.AddLoginAsync(user, loginInfo);
            if (!addLoginResult.Succeeded)
            {
                var isDuplicate = addLoginResult.Errors.Any(e =>
                    e.Code == "LoginAlreadyAssociated"
                );
                if (!isDuplicate)
                {
                    var errors = addLoginResult.Errors.Select(e => e.Description).ToList();
                    return (
                        ApiResponse<AuthResponseDto>.ErrorResponse(
                            "Liên kết tài khoản GitHub thất bại.",
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
                ApiResponse<AuthResponseDto>.ErrorResponse("Tài khoản đã bị vô hiệu hóa.", 403),
                null,
                null
            );
        }

        // ── Bước 4: Cấp token ─────────────────────────────────────────────────
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, accessTokenExpiresAt) = _tokenService.GenerateJwtToken(user, roles);
        var (rawRefreshToken, refreshToken) = await _tokenService.CreateRefreshTokenAsync(user.Id);

        var authResponse = new AuthResponseDto
        {
            AccessToken = accessToken,
            ExpiresAt = accessTokenExpiresAt,
            Email = user.Email!,
            FullName = user.FullName ?? string.Empty,
            Roles = [.. roles],
        };

        return (
            ApiResponse<AuthResponseDto>.SuccessResponse(
                authResponse,
                "Đăng nhập GitHub thành công"
            ),
            rawRefreshToken,
            refreshToken.ExpiresAt
        );
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// POST code lên GitHub để đổi lấy access_token.
    /// </summary>
    private async Task<string?> ExchangeCodeForTokenAsync(string code)
    {
        var client = _httpClientFactory.CreateClient("GitHub");

        var requestBody = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["client_id"] = _settings.ClientId,
                ["client_secret"] = _settings.ClientSecret,
                ["code"] = code,
            }
        );

        // GitHub trả về access_token trong response body (form-encoded hoặc JSON)
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://github.com/login/oauth/access_token"
        )
        {
            Content = requestBody,
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request);
        }
        catch
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        // Nếu GitHub trả về lỗi trong body (ví dụ: bad_verification_code)
        if (doc.RootElement.TryGetProperty("error", out _))
            return null;

        return doc.RootElement.TryGetProperty("access_token", out var tokenEl)
            ? tokenEl.GetString()
            : null;
    }

    /// <summary>
    /// GET /user — lấy profile cơ bản của GitHub user.
    /// </summary>
    private async Task<GitHubUserDto?> GetGitHubUserAsync(string accessToken)
    {
        var client = _httpClientFactory.CreateClient("GitHub");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken
        );

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync("https://api.github.com/user");
        }
        catch
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GitHubUserDto>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
    }

    /// <summary>
    /// GET /user/emails — lấy primary email đã verify (dùng khi email ở /user là null).
    /// </summary>
    private async Task<string?> GetPrimaryEmailAsync(string accessToken)
    {
        var client = _httpClientFactory.CreateClient("GitHub");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken
        );

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync("https://api.github.com/user/emails");
        }
        catch
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        // Ưu tiên: primary + verified → fallback: verified bất kỳ
        string? fallback = null;
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var verified = item.TryGetProperty("verified", out var v) && v.GetBoolean();
            var primary = item.TryGetProperty("primary", out var p) && p.GetBoolean();
            var emailVal = item.TryGetProperty("email", out var e) ? e.GetString() : null;

            if (emailVal is null || !verified)
                continue;

            if (primary)
                return emailVal;

            fallback ??= emailVal;
        }

        return fallback;
    }

    // ── Internal DTO ──────────────────────────────────────────────────────────

    private sealed class GitHubUserDto
    {
        public long Id { get; init; }
        public string? Login { get; init; }
        public string? Name { get; init; }
        public string? Email { get; init; }
    }
}
