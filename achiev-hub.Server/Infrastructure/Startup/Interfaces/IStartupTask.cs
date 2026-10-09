namespace achiev_hub.Server.Infrastructure.Startup.Interfaces;

/// <summary>A startup step that runs only after every <c>IStartupHealthCheck</c> has passed.</summary>
public interface IStartupTask
{
    string Name { get; }

    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
