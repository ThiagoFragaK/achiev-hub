namespace achiev_hub.Server.Infrastructure.HealthChecks.Interfaces;

/// <summary>A dependency check that must pass before the application starts.</summary>
public interface IStartupHealthCheck
{
    string Name { get; }

    Task<StartupHealthCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}
