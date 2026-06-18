namespace auth_service.Extensions;

public static class CorsExtensions
{
    public const string FrontendPolicy = "AllowFrontend";

    public static IServiceCollection AddCorsConfig(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(
                FrontendPolicy,
                policy =>
                {
                    policy
                        .WithOrigins("http://localhost:5500")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials(); // bắt buộc để browser gửi/nhận cookie
                }
            );
        });

        return services;
    }
}
