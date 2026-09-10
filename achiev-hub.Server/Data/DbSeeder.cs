using achiev_hub.Server.Entities;
using achiev_hub.Server.Enums;
using Microsoft.EntityFrameworkCore;

namespace achiev_hub.Server.Data;

public static class DbSeeder
{
    public const string SeedPassword = "achiev456";

    public static async Task SeedAsync(ApplicationDbContext db)
    {
        await db.Database.MigrateAsync();

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(SeedPassword);
        var seedUsers = new[]
        {
            new { Email = "user@example.com", SteamId = "76561198000000001", Role = "user" },
            new { Email = "admin@example.com", SteamId = "76561198000000002", Role = "admin" }
        };

        foreach (var seed in seedUsers)
        {
            var user = await db.Users.FirstOrDefaultAsync(u =>
                u.Email == seed.Email || u.SteamId == seed.SteamId);

            if (user is null)
            {
                db.Users.Add(new User
                {
                    Email = seed.Email,
                    SteamId = seed.SteamId,
                    Password = passwordHash,
                    Role = seed.Role,
                    Status = (int)StatusEnum.Active,
                    IsEmailVerified = true,
                    TokenVersion = 0
                });
            }
            else
            {
                user.Email = seed.Email;
                user.SteamId = seed.SteamId;
                user.Role = seed.Role;
                user.Status = (int)StatusEnum.Active;
                user.IsEmailVerified = true;
                user.Password = passwordHash;
            }
        }

        await db.SaveChangesAsync();
    }
}
