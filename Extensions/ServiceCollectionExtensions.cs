using auth_service.Interfaces;
using auth_service.Response;
using auth_service.Services;
using Microsoft.AspNetCore.Mvc;

namespace auth_service.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IRecoveryCodeService, RecoveryCodeService>();
        services.AddScoped<ITwoFactorService, TwoFactorService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddScoped<IGitHubAuthService, GitHubAuthService>();
        services.AddScoped<ICookieService, CookieService>();

        // GitHub API client — User-Agent bắt buộc theo GitHub API policy
        services.AddHttpClient(
            "GitHub",
            client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("auth-service/1.0");
                client.Timeout = TimeSpan.FromSeconds(15);
            }
        );

        return services;
    }

    public static IServiceCollection AddControllerConfig(this IServiceCollection services)
    {
        services
            .AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                // Trả về ApiResponse<object> thay vì format mặc định của ASP.NET
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context
                        .ModelState.Where(x => x.Value?.Errors.Count > 0)
                        .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage))
                        .ToList();

                    var response = ApiResponse<object>.ErrorResponse(
                        "Dữ liệu không hợp lệ",
                        400,
                        errors
                    );

                    return new BadRequestObjectResult(response);
                };
            });

        return services;
    }
}
