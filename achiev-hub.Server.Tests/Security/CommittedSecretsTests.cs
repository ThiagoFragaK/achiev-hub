using System.Text.Json;
using System.Text.RegularExpressions;
using achiev_hub.Server.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace achiev_hub.Server.Tests.Security;

public class CommittedSecretsTests
{
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "achiev-hub.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate achiev-hub.slnx from test base directory.");
    }

    [Theory]
    [InlineData("achiev-hub.Server/appsettings.json")]
    [InlineData("achiev-hub.Server/appsettings.Development.json")]
    public void Tracked_appsettings_must_not_embed_steam_api_key(string relativePath)
    {
        var path = Path.Combine(FindRepoRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(path), $"Missing {path}");

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var apiKey = doc.RootElement.GetProperty("SteamApi").GetProperty("ApiKey").GetString() ?? "";

        Assert.True(
            string.IsNullOrWhiteSpace(apiKey),
            "SteamApi:ApiKey must be empty in tracked appsettings; use user secrets or environment variables.");
        Assert.False(Regex.IsMatch(apiKey, "^[0-9A-Fa-f]{32}$"), "Steam API key hex literal must not be committed.");
    }

    [Fact]
    public void Validate_throws_when_steam_api_key_missing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SteamApi:ApiKey"] = "",
                ["Jwt:Key"] = "ThisIsAStrongEnoughJwtSigningKey_32b+"
            })
            .Build();

        var env = new FakeHostEnvironment { EnvironmentName = Environments.Development };

        var ex = Assert.Throws<InvalidOperationException>(() => SecurityConfiguration.Validate(config, env));
        Assert.Contains("SteamApi:ApiKey", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_throws_on_dev_jwt_placeholder_outside_development()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SteamApi:ApiKey"] = "0123456789ABCDEF0123456789ABCDEF",
                ["Jwt:Key"] = "AchievHubDevSecretKey_ChangeInProduction_Min32Chars!"
            })
            .Build();

        var env = new FakeHostEnvironment { EnvironmentName = Environments.Production };

        var ex = Assert.Throws<InvalidOperationException>(() => SecurityConfiguration.Validate(config, env));
        Assert.Contains("Jwt:Key", ex.Message, StringComparison.Ordinal);
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
