using achiev_hub.Server.Infrastructure.HealthChecks.Interfaces;
using achiev_hub.Server.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace achiev_hub.Server.Infrastructure.HealthChecks;

public sealed class DatabaseHealthCheck : IStartupHealthCheck
{
    private const string DatabaseDoesNotExistSqlState = "3D000";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(IServiceScopeFactory scopeFactory, ILogger<DatabaseHealthCheck> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public string Name => "Database";

    public async Task<StartupHealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            // OpenConnection (not CanConnect) so the provider's exception surfaces with the actual cause.
            await db.Database.OpenConnectionAsync(cancellationToken);
            await db.Database.CloseConnectionAsync();
            return StartupHealthCheckResult.Healthy();
        }
        catch (PostgresException ex) when (ex.SqlState == DatabaseDoesNotExistSqlState)
        {
            // Server is reachable and credentials are valid; migrations create the database.
            _logger.LogWarning("Database does not exist yet; it will be created by migrations.");
            return StartupHealthCheckResult.Healthy();
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return StartupHealthCheckResult.Unhealthy(
                $"{ex.Message} Check ConnectionStrings__DefaultConnection Host, Port, Database, Username, and Password.",
                ex);
        }
    }
}
