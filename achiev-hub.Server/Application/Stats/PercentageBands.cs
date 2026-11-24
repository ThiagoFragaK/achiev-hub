namespace achiev_hub.Server.Application.Stats;

/// <summary>
/// Shared completion-percentage band keys, labels, and range matching.
/// Keep in sync with SQL counts in UsersGameRepository.
/// </summary>
public static class PercentageBands
{
    public static IReadOnlyList<PercentageBandDefinition> All { get; } =
    [
        new("100", "100%", p => p == 100m),
        new("gt80", "> 80%", p => p > 80m && p < 100m),
        new("gt60", "> 60%", p => p > 60m && p <= 80m),
        new("gt50", "> 50%", p => p > 50m && p <= 60m),
        new("gt30", "> 30%", p => p > 30m && p <= 50m),
        new("gt10", "> 10%", p => p > 10m && p <= 30m),
        new("gt0", "> 0%", p => p > 0m && p <= 10m),
        new("0", "0%", p => p == 0m)
    ];

    public static string ResolveKey(decimal percentage)
    {
        foreach (var band in All)
        {
            if (band.Match(percentage))
            {
                return band.Key;
            }
        }

        return "0";
    }
}

public sealed record PercentageBandDefinition(
    string Key,
    string Label,
    Func<decimal, bool> Match);
