using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using achiev_hub.Server.Application.Achievements;
using achiev_hub.Server.Application.Achievements.Interfaces;
using achiev_hub.Server.Application.Auth;
using achiev_hub.Server.Application.Auth.Interfaces;
using achiev_hub.Server.Application.Common;
using achiev_hub.Server.Application.Common.Interfaces;
using achiev_hub.Server.Application.Games;
using achiev_hub.Server.Application.Games.Interfaces;
using achiev_hub.Server.Application.Goals;
using achiev_hub.Server.Application.Goals.Interfaces;
using achiev_hub.Server.Application.Stats;
using achiev_hub.Server.Application.Stats.Interfaces;
using achiev_hub.Server.Application.Steam;
using achiev_hub.Server.Application.Steam.Interfaces;
using achiev_hub.Server.Application.Users;
using achiev_hub.Server.Application.Users.Interfaces;
using achiev_hub.Server.Domain.Entities;
using achiev_hub.Server.Domain.Interfaces;
using achiev_hub.Server.Infrastructure.Auth;
using achiev_hub.Server.Infrastructure.Email;
using achiev_hub.Server.Infrastructure.HealthChecks;
using achiev_hub.Server.Infrastructure.HealthChecks.Interfaces;
using achiev_hub.Server.Infrastructure.Messaging;
using achiev_hub.Server.Infrastructure.Messaging.Interfaces;
using achiev_hub.Server.Infrastructure.Options;
using achiev_hub.Server.Infrastructure.Persistence;
using achiev_hub.Server.Infrastructure.Startup;
using achiev_hub.Server.Infrastructure.Startup.Interfaces;
using achiev_hub.Server.Infrastructure.Steam;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

builder.Services.Configure<SteamApiOptions>(builder.Configuration.GetSection(SteamApiOptions.SectionName));
builder.Services.Configure<SendGridOptions>(builder.Configuration.GetSection(SendGridOptions.SectionName));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<StartupHealthCheckOptions>(builder.Configuration.GetSection(StartupHealthCheckOptions.SectionName));
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddHttpClient<ISteamApiClient, SteamApiClient>();
builder.Services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
builder.Services.AddSingleton<ISteamSyncClient, SteamSyncClient>();

// Startup sequence. Health checks run in registration order: Database first, then RabbitMQ.
builder.Services.AddSingleton<IStartupHealthCheck, DatabaseHealthCheck>();
builder.Services.AddSingleton<IStartupHealthCheck, RabbitMqHealthCheck>();
builder.Services.AddSingleton<IStartupTask, RabbitMqTopologyInitializer>();
builder.Services.AddSingleton<StartupOrchestrator>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMemoryCache();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IPlayersService, PlayersService>();
builder.Services.AddScoped<IGamesService, GamesService>();
builder.Services.AddScoped<ISteamSyncService, SteamSyncService>();
builder.Services.AddScoped<ISyncStatusService, SyncStatusService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserStatsService, UserStatsService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IEmailSender, SendGridEmailSender>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IAchievementService, AchievementService>();
builder.Services.AddScoped<IGoalService, GoalService>();
builder.Services.AddScoped<IUsersGameService, UsersGameService>();
builder.Services.AddScoped<IUsersAchievementService, UsersAchievementService>();
builder.Services.AddScoped<IGoalAchievementService, GoalAchievementService>();

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
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

var app = builder.Build();

if (!await app.Services.GetRequiredService<StartupOrchestrator>().RunAsync())
{
    app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup")
        .LogCritical("Startup checks failed; shutting down.");
    Environment.ExitCode = 1;
    return;
}

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DbSeeder.SeedAsync(db);
}
catch (Exception ex) when (ex is Npgsql.NpgsqlException or InvalidOperationException)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    logger.LogError(ex,
        "Database startup failed. Check ConnectionStrings__DefaultConnection (or appsettings) Host, Port, " +
        "Database, Username, and Password.");
    throw;
}

app.UseDefaultFiles();
app.MapStaticAssets();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
