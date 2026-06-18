using System.Text;
using auth_service.Response;
using auth_service.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace auth_service.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var jwtSettings =
            configuration.GetSection("JwtSettings").Get<JwtSettings>()
            ?? throw new InvalidOperationException("JwtSettings is not configured.");

        var googleSettings =
            configuration.GetSection("GoogleOAuth").Get<GoogleOAuthSettings>()
            ?? new GoogleOAuthSettings();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.SecretKey)
                    ),
                    ClockSkew = TimeSpan.Zero,
                };

                options.Events = new JwtBearerEvents
                {
                    // 401 – không có token hoặc token không hợp lệ
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";

                        var response = new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Bạn chưa đăng nhập hoặc token không hợp lệ",
                            StatusCode = 401,
                            Errors = context.AuthenticateFailure is not null
                                ? [context.AuthenticateFailure.Message]
                                : null,
                        };

                        await context.Response.WriteAsJsonAsync(response);
                    },

                    // 403 – đã đăng nhập nhưng không đủ quyền
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = 403;
                        context.Response.ContentType = "application/json";

                        var response = new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Bạn không có quyền thực hiện thao tác này",
                            StatusCode = 403,
                        };

                        await context.Response.WriteAsJsonAsync(response);
                    },
                };
            })
            .AddGoogle(options =>
            {
                options.ClientId = googleSettings.ClientId;
                options.ClientSecret = googleSettings.ClientSecret;
                // Path này được middleware xử lý trực tiếp (không qua controller)
                // Sau khi xử lý xong sẽ redirect đến RedirectUri trong AuthenticationProperties
                options.CallbackPath = "/signin-google";
                // Yêu cầu thêm profile + email scope (mặc định đã có, khai báo tường minh)
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.SaveTokens = false;
                // Sau khi Google xác thực xong, lưu principal vào ExternalCookie
                // để controller có thể đọc qua AuthenticateAsync(IdentityConstants.ExternalScheme)
                options.SignInScheme = IdentityConstants.ExternalScheme;
            });

        return services;
    }
}
