using Aspire.Hosting;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Npgsql;
using TUnit.Core.Interfaces;

namespace MicroserviceTemplate.Tests.Common;

public class IntegrationTestFixture : IAsyncInitializer, IAsyncDisposable
{
    private const string ApiServiceName = "apiservice";
    private const string ApiEndpointName = "http";
    private const string PostgresDbName = "postgresdb";
    private static readonly TimeSpan AppHostTimeout = IsContinuousIntegration
        ? TimeSpan.FromMinutes(5)
        : TimeSpan.FromMinutes(3);
    private static readonly TimeSpan DockerProbeTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan DatabaseReadyTimeout = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan DatabasePollInterval = TimeSpan.FromSeconds(2);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private DistributedApplication? app;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        if (!IsDockerAvailable())
        {
            TUnit.Core.Skip.Test("Docker is required for integration tests. Start Docker to run these tests.");
        }

        using CancellationTokenSource timeout = new(AppHostTimeout);
        IDistributedApplicationTestingBuilder appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.MicroserviceTemplate_AppHost>(
            cancellationToken: timeout.Token);

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
        appHost.Services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.AddFilter("Default", LogLevel.Information);
            logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
            logging.AddFilter("Aspire.Hosting.Dcp", LogLevel.Warning);
        });

        app = await appHost.BuildAsync(timeout.Token)
            .WaitAsync(AppHostTimeout, timeout.Token);
        await app.StartAsync(timeout.Token)
            .WaitAsync(AppHostTimeout, timeout.Token);

        Client = app.CreateHttpClient(ApiServiceName, ApiEndpointName);
        await app.ResourceNotifications.WaitForResourceHealthyAsync(ApiServiceName, timeout.Token);

        await EnsureDatabaseReadyAsync();
    }

    public Task<T> PostAsync<T>(string url, object body, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        where T : class
        => SendAndReadAsync<T>(HttpMethod.Post, url, body, expectedStatusCode);

    public Task<T> PutAsync<T>(string url, object body, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        where T : class
        => SendAndReadAsync<T>(HttpMethod.Put, url, body, expectedStatusCode);

    public Task<T> GetAsync<T>(string url, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        where T : class
        => SendAndReadAsync<T>(HttpMethod.Get, url, null, expectedStatusCode);

    public Task<HttpResponseMessage> SendAsJsonAsync(HttpMethod method, string url, object body)
    {
        HttpRequestMessage request = new(method, url)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        return Client.SendAsync(request);
    }

    public Task<HttpResponseMessage> SendAsync(HttpMethod method, string url)
    {
        return Client.SendAsync(new HttpRequestMessage(method, url));
    }

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response)
        where T : class
    {
        T? value = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        value.ShouldNotBeNull();
        return value;
    }

    private async Task<T> SendAndReadAsync<T>(
        HttpMethod method,
        string url,
        object? body,
        HttpStatusCode expectedStatusCode)
        where T : class
    {
        using HttpResponseMessage response = body is null
            ? await SendAsync(method, url)
            : await SendAsJsonAsync(method, url, body);

        await response.StatusCode.ShouldBeWithBodyAsync(expectedStatusCode, response);
        return await ReadAsync<T>(response);
    }

    private static bool IsDockerAvailable()
    {
        try
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = "docker",
                Arguments = "info",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process? process = Process.Start(startInfo);
            if (process is null)
            {
                return false;
            }

            return process.WaitForExit(DockerProbeTimeout) && process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsContinuousIntegration =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CI"));

    private async Task EnsureDatabaseReadyAsync()
    {
        string? postgresConnectionString = await app!.GetConnectionStringAsync(PostgresDbName);
        if (string.IsNullOrWhiteSpace(postgresConnectionString))
        {
            throw new InvalidOperationException("Postgres connection string not available.");
        }

        await WaitForDatabaseTablesAsync(postgresConnectionString, DatabaseReadyTimeout);
    }

    private static async Task WaitForDatabaseTablesAsync(string connectionString, TimeSpan timeout)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            try
            {
                await using NpgsqlConnection connection = new(connectionString);
                await connection.OpenAsync();
                await using NpgsqlCommand command = connection.CreateCommand();
                command.CommandText = """
                    SELECT EXISTS (
                        SELECT 1
                        FROM information_schema.tables
                        WHERE table_schema = 'public'
                          AND table_type = 'BASE TABLE'
                    )
                    """;
                object? result = await command.ExecuteScalarAsync();
                if (result is true)
                {
                    return;
                }
            }
            catch
            {
                // Ignore transient startup failures while database is initializing.
            }

            await Task.Delay(DatabasePollInterval);
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
