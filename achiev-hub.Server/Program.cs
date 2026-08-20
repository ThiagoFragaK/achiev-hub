using achiev_hub.Server.Data;
using achiev_hub.Server.Options;
using achiev_hub.Server.Repositories;
using achiev_hub.Server.Repositories.Interfaces;
using achiev_hub.Server.Services;
using achiev_hub.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SteamApiOptions>(builder.Configuration.GetSection(SteamApiOptions.SectionName));
builder.Services.AddHttpClient<ISteamRepository, SteamRepository>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IPlayersService, PlayersService>();
builder.Services.AddScoped<IGamesService, GamesService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IAchievementService, AchievementService>();
builder.Services.AddScoped<IGoalService, GoalService>();
builder.Services.AddScoped<IUsersGameService, UsersGameService>();
builder.Services.AddScoped<IUsersAchievementService, UsersAchievementService>();
builder.Services.AddScoped<IGoalAchievementService, GoalAchievementService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseDefaultFiles();
app.MapStaticAssets();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
