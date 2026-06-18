using auth_service.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace auth_service.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, SameUserHandler>();

        services
            .AddAuthorizationBuilder()
            .AddPolicy(
                Permissions.UsersRead,
                p => p.RequireClaim("permission", Permissions.UsersRead)
            )
            .AddPolicy(
                Permissions.UsersDelete,
                p => p.RequireClaim("permission", Permissions.UsersDelete)
            )
            .AddPolicy(Permissions.ProfileEdit, p => p.AddRequirements(new SameUserRequirement()));

        return services;
    }
}
