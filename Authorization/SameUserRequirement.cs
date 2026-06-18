using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace auth_service.Authorization;

/// <summary>
/// Requirement: user chỉ được thao tác trên resource của chính mình.
/// Controller cần truyền targetUserId qua IAuthorizationService.AuthorizeAsync.
/// </summary>
public class SameUserRequirement : IAuthorizationRequirement { }

public class SameUserHandler : AuthorizationHandler<SameUserRequirement, string>
{
    /// <param name="resource">targetUserId — ID của user cần thao tác</param>
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SameUserRequirement requirement,
        string resource
    )
    {
        var requesterId =
            context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue(
                System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub
            );

        if (requesterId == resource)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
