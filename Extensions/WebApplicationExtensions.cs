using auth_service.Response;
using Microsoft.AspNetCore.Identity;

namespace auth_service.Extensions;

public static class WebApplicationExtensions
{
    public static async Task SeedRolesAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in new[] { "Customer", "Employee", "Admin" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    public static WebApplication ConfigureMiddleware(this WebApplication app)
    {
        // ── Global Exception Handler ──────────────────────────────────────────
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";

                var response = new ApiResponse<object>
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi nội bộ",
                    StatusCode = 500,
                };

                // Thêm chi tiết lỗi trong môi trường Development
                var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
                if (env.IsDevelopment())
                {
                    var exceptionFeature =
                        context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
                    if (exceptionFeature?.Error is not null)
                    {
                        response.Errors = [exceptionFeature.Error.Message];
                    }
                }

                await context.Response.WriteAsJsonAsync(response);
            });
        });

        // ── Middleware Pipeline ───────────────────────────────────────────────
        app.UseCors(CorsExtensions.FrontendPolicy);
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseSwagger();
        app.UseSwaggerUI();

        // ── Endpoints ─────────────────────────────────────────────────────────
        app.MapControllers();
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        // Fallback: trả 404 cho mọi route không khớp
        app.MapFallback(() =>
            Results.Json(
                new
                {
                    success = false,
                    message = "Endpoint không tồn tại",
                    statusCode = 404,
                },
                statusCode: 404
            )
        );

        return app;
    }
}
