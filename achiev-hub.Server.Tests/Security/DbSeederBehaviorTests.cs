using achiev_hub.Server.Infrastructure.Persistence;

namespace achiev_hub.Server.Tests.Security;

public class DbSeederBehaviorTests
{
    [Fact]
    public void SeedPassword_is_only_for_development_bootstrap()
    {
        // Documents intended invariant: production SeedAsync returns before touching passwords.
        // Full EF migration tests are integration-scoped; this guards the public contract.
        Assert.False(string.IsNullOrWhiteSpace(DbSeeder.SeedPassword));
        var source = File.ReadAllText(
            Path.Combine(
                FindRepoRoot(),
                "achiev-hub.Server",
                "Infrastructure",
                "Persistence",
                "DbSeeder.cs"));
        Assert.Contains("if (!environment.IsDevelopment())", source, StringComparison.Ordinal);
        Assert.Contains("if (exists)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("user.Password = passwordHash;", source);
    }

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

        throw new InvalidOperationException("repo root not found");
    }
}
