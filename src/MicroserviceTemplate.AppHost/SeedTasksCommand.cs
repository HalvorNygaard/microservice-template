using System.Net.Http.Json;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting;

internal static class SeedTasksCommand
{
    internal static IResourceBuilder<ProjectResource> WithSeedTasksCommand(
        this IResourceBuilder<ProjectResource> builder)
    {
        EndpointReference httpEndpoint = builder.GetEndpoint("http");
        return builder.WithCommand(
            name: "seed-tasks",
            displayName: "Seed tasks",
            executeCommand: context => SeedAsync(httpEndpoint, context),
            commandOptions: new CommandOptions
            {
                Description = "Creates a few local sample tasks through the public API.",
                IconName = "Database",
                IsHighlighted = true,
                UpdateState = context => context.ResourceSnapshot.HealthStatus is HealthStatus.Healthy
                    ? ResourceCommandState.Enabled
                    : ResourceCommandState.Disabled
            });
    }

    private static async Task<ExecuteCommandResult> SeedAsync(
        EndpointReference httpEndpoint,
        ExecuteCommandContext context)
    {
        try
        {
            string? baseAddress = await httpEndpoint.GetValueAsync(context.CancellationToken);
            if (string.IsNullOrWhiteSpace(baseAddress))
            {
                return CommandResults.Failure("The API endpoint is not available yet.");
            }

            using HttpClient client = new() { BaseAddress = new Uri(baseAddress) };
            using HttpResponseMessage listResponse = await client.GetAsync(
                new Uri("/api/v1/tasks?pageSize=100", UriKind.Relative),
                context.CancellationToken);
            if (!listResponse.IsSuccessStatusCode)
            {
                return CommandResults.Failure($"Could not inspect existing tasks. HTTP {(int)listResponse.StatusCode}.");
            }

            TaskPage? page = await listResponse.Content.ReadFromJsonAsync<TaskPage>(context.CancellationToken);
            HashSet<string> existingTitles = page?.Items
                .Select(static task => task.Title)
                .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
            int createdCount = 0;

            foreach (TaskSeed seed in CreateSeeds().Where(seed => !existingTitles.Contains(seed.Title)))
            {
                using HttpResponseMessage response = await client.PostAsJsonAsync(
                    new Uri("/api/v1/tasks", UriKind.Relative),
                    seed,
                    context.CancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    string body = await response.Content.ReadAsStringAsync(context.CancellationToken);
                    return CommandResults.Failure(
                        $"Failed to seed '{seed.Title}'. HTTP {(int)response.StatusCode}: {body}");
                }

                createdCount++;
            }

            return CommandResults.Success(createdCount == 0
                ? "The sample tasks already exist."
                : $"Created {createdCount} sample task(s) through the public API.");
        }
        catch (Exception exception)
        {
            return CommandResults.Failure(exception);
        }
    }

    private static IReadOnlyList<TaskSeed> CreateSeeds()
    {
        DateTimeOffset now = TimeProvider.System.GetUtcNow();
        return
        [
            new("Review service boundary", "Confirm what this microservice owns before adding integrations.", now.AddDays(2)),
            new("Add the first feature", "Keep endpoint mapping, contracts, and behavior together in one operation slice.", now.AddDays(5)),
            new("Prepare production rollout", "Define migration, telemetry, health, rollback, and ownership before deployment.", now.AddDays(10))
        ];
    }

    private sealed record TaskSeed(string Title, string Description, DateTimeOffset? DueDate);
    private sealed record TaskSummary(string Title);
    private sealed record TaskPage(IReadOnlyList<TaskSummary> Items);
}
