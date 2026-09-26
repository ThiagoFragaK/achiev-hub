using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using achiev_hub.Server.Data;
using achiev_hub.Server.Entities;
using achiev_hub.Server.Options;
using achiev_hub.Server.Repositories;
using achiev_hub.Server.Repositories.Interfaces;
using achiev_hub.Server.Services;
using achiev_hub.Server.Services.Interfaces;
using achiev_hub.Server.Support;
using achiev_hub.Server.Workers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile(
        "appsettings.Development.local.json",
        optional: true,
        reloadOnChange: true);
}

var appRole = builder.Configuration["SyncWorker:AppRole"]
    ?? builder.Configuration["AppRole"]
    ?? "All";
appRole = appRole.Trim();
var runApi = appRole.Equals("Api", StringComparison.OrdinalIgnoreCase)
    || appRole.Equals("All", StringComparison.OrdinalIgnoreCase);
var runWorker = appRole.Equals("Worker", StringComparison.OrdinalIgnoreCase)
    || appRole.Equals("All", StringComparison.OrdinalIgnoreCase);

if (!runApi && !runWorker)
{
    throw new InvalidOperationException("SyncWorker:AppRole must be Api, Worker, or All.");
}

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
var steamApiKey = builder.Configuration.GetSection(SteamApiOptions.SectionName).Get<SteamApiOptions>()?.ApiKey
    ?? string.Empty;

if (string.IsNullOrWhiteSpace(jwtSettings.Key) || jwtSettings.Key.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key must be configured and at least 32 characters. " +
        "Set it via user-secrets, environment variable Jwt__Key, or appsettings.Development.local.json.");
}

if (string.IsNullOrWhiteSpace(steamApiKey))
{
    throw new InvalidOperationException(
        "SteamApi:ApiKey must be configured. " +
        "Set it via user-secrets, environment variable SteamApi__ApiKey, or appsettings.Development.local.json.");
}

builder.Services.Configure<SteamApiOptions>(builder.Configuration.GetSection(SteamApiOptions.SectionName));
builder.Services.Configure<SendGridOptions>(builder.Configuration.GetSection(SendGridOptions.SectionName));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<SyncWorkerOptions>(builder.Configuration.GetSection(SyncWorkerOptions.SectionName));
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<SteamApiThrottle>();
builder.Services.AddHttpClient<ISteamRepository, SteamRepository>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMemoryCache();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IPlayersService, PlayersService>();
builder.Services.AddScoped<IGamesService, GamesService>();
builder.Services.AddScoped<ISteamSyncService, SteamSyncService>();
builder.Services.AddScoped<ISyncJobEnqueueService, SyncJobEnqueueService>();
builder.Services.AddScoped<ISyncStatusService, SyncStatusService>();
builder.Services.AddScoped<SyncJobProcessor>();
builder.Services.AddScoped<IUserStatsService, UserStatsService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IEmailSender, SendGridEmailSender>();

if (runWorker)
{
    builder.Services.AddHostedService<SyncWorkerBackgroundService>();
}

if (runApi)
{
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("auth", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
        options.AddPolicy("steam", httpContext =>
        {
            var userKey = httpContext.User.FindFirstValue("steam_id")
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(
                userKey,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 60,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
        });
    });

    var key = Encoding.UTF8.GetBytes(jwtSettings.Key);

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var isGuest = context.Principal?.IsInRole(JwtTokenService.GuestRole) == true
                        || context.Principal?.FindFirstValue(JwtTokenService.TokenKindClaim) == JwtTokenService.GuestTokenKind;
                    if (isGuest)
                    {
                        return;
                    }

                    var userIdClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
                    var tokenVersionClaim = context.Principal?.FindFirstValue("token_version");

                    if (!int.TryParse(userIdClaim, out var userId) || !int.TryParse(tokenVersionClaim, out var tokenVersion))
                    {
                        context.Fail("Invalid token claims.");
                        return;
                    }

                    var users = context.HttpContext.RequestServices.GetRequiredService<IRepository<User>>();
                    var user = await users.FirstOrDefaultAsync(u => u.Id == userId, trackChanges: false);
                    if (user is null || user.TokenVersion != tokenVersion)
                    {
                        context.Fail("Token has been revoked.");
                    }
                }
            };
        });

    builder.Services.AddAuthorization();
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
}

var app = builder.Build();

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DbSeeder.SeedAsync(db, app.Environment);
}
catch (Exception ex) when (ex is Npgsql.NpgsqlException or InvalidOperationException)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    logger.LogError(ex,
        "Database startup failed. Check ConnectionStrings__DefaultConnection (or appsettings) Host, Port, " +
        "Database, Username, and Password.");
    throw;
}

if (runApi)
{
    app.UseDefaultFiles();
    app.MapStaticAssets();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();
    app.MapControllers();
    app.MapFallbackToFile("/index.html");
}
else
{
    // Worker-only process: expose a minimal health endpoint for Docker/Railway probes.
    app.MapGet("/health", () => Results.Ok(new { role = "Worker", status = "ok" }));
}

app.Logger.LogInformation("Starting with AppRole={AppRole} (api={RunApi}, worker={RunWorker})", appRole, runApi, runWorker);

app.Run();
