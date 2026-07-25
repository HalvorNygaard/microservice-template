using TUnit.Core.Interfaces;

namespace ModernMicroservice.IntegrationTests.Common;

public sealed class IntegrationTestFixture : IAsyncInitializer, IAsyncDisposable
{
    private const string ApiServiceName = "apiservice";
    private const string ApiEndpointName = "http";
    private static readonly TimeSpan AppHostTimeout = TimeSpan.FromMinutes(3);

    private DistributedApplication? app;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        using CancellationTokenSource timeout = new(AppHostTimeout);
        IDistributedApplicationTestingBuilder appHost =
            await DistributedApplicationTestingBuilder.CreateAsync<ModernMicroservice.AppHost.AssemblyMarker>(
                cancellationToken: timeout.Token);

        appHost.Services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.AddFilter("Default", LogLevel.Information);
            logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
            logging.AddFilter("Aspire.Hosting.Dcp", LogLevel.Warning);
        });

        app = await appHost.BuildAsync(timeout.Token);
        await app.StartAsync(timeout.Token);

        Client = app.CreateHttpClient(ApiServiceName, ApiEndpointName);
        await app.ResourceNotifications.WaitForResourceHealthyAsync(ApiServiceName, timeout.Token);
    }

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response)
        where T : class
    {
        T? value = await response.Content.ReadFromJsonAsync<T>();
        value.ShouldNotBeNull();
        return value;
    }

    public async ValueTask DisposeAsync()
    {
        Client?.Dispose();
        if (app is not null)
        {
            await app.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
