using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using achiev_hub.Server.Data;
using achiev_hub.Server.Entities;
using achiev_hub.Server.Options;
using achiev_hub.Server.Repositories;
using achiev_hub.Server.Repositories.Interfaces;
using achiev_hub.Server.Services;
using achiev_hub.Server.Services.Interfaces;
using achiev_hub.Server.Support;
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
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddHttpClient<ISteamRepository, SteamRepository>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IPlayersService, PlayersService>();
builder.Services.AddScoped<IGamesService, GamesService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
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
