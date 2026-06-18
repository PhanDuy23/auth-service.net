using auth_service.Extensions;
using auth_service.Settings;
using DotNetEnv;

Env.Load();
var builder = WebApplication.CreateBuilder(args);

// ── Settings ──────────────────────────────────────────────────────────────────
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<GoogleOAuthSettings>(builder.Configuration.GetSection("GoogleOAuth"));
builder.Services.Configure<GitHubOAuthSettings>(builder.Configuration.GetSection("GitHubOAuth"));

// ── Service Registration ──────────────────────────────────────────────────────
builder
    .Services.AddDatabase(builder.Configuration)
    .AddIdentityConfig(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddAuthorizationPolicies()
    .AddSwaggerConfig()
    .AddCorsConfig()
    .AddApplicationServices()
    .AddControllerConfig();

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

await app.SeedRolesAsync();
app.ConfigureMiddleware();
app.Run();
