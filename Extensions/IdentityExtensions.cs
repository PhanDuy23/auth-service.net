using auth_service.Data;
using auth_service.Models;
using auth_service.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace auth_service.Extensions;

public static class IdentityExtensions
{
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
        );

        return services;
    }

    public static IServiceCollection AddIdentityConfig(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var identitySettings = configuration.GetSection("IdentitySettings");

        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            options.TokenLifespan = TimeSpan.FromHours(1);
        });

        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = identitySettings.GetValue<bool>("RequireDigit");
                options.Password.RequireLowercase = identitySettings.GetValue<bool>(
                    "RequireLowercase"
                );
                options.Password.RequireUppercase = identitySettings.GetValue<bool>(
                    "RequireUppercase"
                );
                options.Password.RequiredLength = identitySettings.GetValue<int>("RequiredLength");
                options.Lockout.MaxFailedAccessAttempts = identitySettings.GetValue<int>(
                    "MaxFailedAccessAttempts"
                );
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(
                    identitySettings.GetValue<int>("DefaultLockoutMinutes")
                );
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }
}
