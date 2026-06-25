using Aspire.Hosting;
using System.Diagnostics;
using Npgsql;
using TUnit.Core.Interfaces;

namespace MicroserviceTemplate.Tests.Common;

public class IntegrationTestFixture : IAsyncInitializer, IAsyncDisposable
{
    private const string ApiServiceName = "apiservice";
    private const string ApiEndpointName = "http";
    private const string PostgresDbName = "postgresdb";
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(3);

    private DistributedApplication? app;
    private string? postgresConnectionString;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        if (!IsDockerAvailable())
        {
            TUnit.Core.Skip.Test("Docker is required for integration tests. Start Docker to run these tests.");
        }

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.MicroserviceTemplate_AppHost>();

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        app = await appHost.BuildAsync();
        await app.StartAsync().WaitAsync(DefaultTimeout);

        Client = app.CreateHttpClient(ApiServiceName, ApiEndpointName);
        await app.ResourceNotifications.WaitForResourceHealthyAsync(ApiServiceName).WaitAsync(DefaultTimeout);

        await EnsureDatabaseReadyAsync();
    }

    private static bool IsDockerAvailable()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "info",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return false;
            }

            return process.WaitForExit(5000) && process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task EnsureDatabaseReadyAsync()
    {
        postgresConnectionString = await app!.GetConnectionStringAsync(PostgresDbName);
        if (string.IsNullOrWhiteSpace(postgresConnectionString))
        {
            throw new InvalidOperationException("Postgres connection string not available.");
        }

        await WaitForDatabaseTablesAsync(postgresConnectionString, TimeSpan.FromMinutes(1));
    }

    private static async Task WaitForDatabaseTablesAsync(string connectionString, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            try
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = """
                    SELECT COUNT(*)
                    FROM information_schema.tables
                    WHERE table_schema = 'public'
                      AND table_type = 'BASE TABLE'
                    """;
                var result = await command.ExecuteScalarAsync();
                if (result is long count && count > 0)
                {
                    return;
                }
            }
            catch
            {
                // Ignore transient startup failures while database is initializing.
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        throw new TimeoutException("Timed out waiting for database tables to be created.");
    }

    public async ValueTask DisposeAsync()
    {
        Client?.Dispose();
        if (app is not null)
        {
            await app.DisposeAsync();
        }
    }
}
