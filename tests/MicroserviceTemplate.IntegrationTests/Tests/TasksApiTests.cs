using System.Text;
using System.Text.Json;
using ModernMicroservice.Features.Tasks;
using ModernMicroservice.Features.Tasks.Create;
using ModernMicroservice.IntegrationTests.Common;

namespace ModernMicroservice.IntegrationTests;

[ClassDataSource<IntegrationTestFixture>(Shared = SharedType.PerTestSession)]
public sealed class TasksApiTests(IntegrationTestFixture fixture)
{
    private const string TasksPath = "/api/v1/tasks";
    private const string ProblemTypeRoot = "/problems/";
    private const string ServiceProblemTypeRoot = ProblemTypeRoot + "apiservice/";
    private HttpClient Client => fixture.Client;

    [Test]
    public async Task CreateAndGetTask()
    {
        TaskRepresentation created = await CreateTaskAsync("Read the architecture guide");

        using HttpResponseMessage response = await Client.GetAsync($"{TasksPath}/{created.Id}");

        await response.ShouldBeStatusAsync(HttpStatusCode.OK);
        TaskRepresentation read = await IntegrationTestFixture.ReadAsync<TaskRepresentation>(response);
        read.Id.ShouldBe(created.Id);
        read.Title.ShouldBe(created.Title);
        read.Description.ShouldBe(created.Description);
        read.Status.ShouldBe(created.Status);
        Math.Abs((read.CreatedAt - created.CreatedAt).TotalMilliseconds).ShouldBeLessThan(1);
        Math.Abs((read.UpdatedAt - created.UpdatedAt).TotalMilliseconds).ShouldBeLessThan(1);
    }

    [Test]
    public async Task CompleteTaskIsIdempotent()
    {
        TaskRepresentation created = await CreateTaskAsync("Complete the reference task");

        using HttpResponseMessage firstResponse = await Client.PostAsync(
            $"{TasksPath}/{created.Id}/complete",
            null);
        await firstResponse.ShouldBeStatusAsync(HttpStatusCode.OK);
        TaskRepresentation first = await IntegrationTestFixture.ReadAsync<TaskRepresentation>(firstResponse);
        first.Status.ShouldBe(TaskItemStatus.Done);

        using HttpResponseMessage secondResponse = await Client.PostAsync(
            $"{TasksPath}/{created.Id}/complete",
            null);
        await secondResponse.ShouldBeStatusAsync(HttpStatusCode.OK);
        TaskRepresentation second = await IntegrationTestFixture.ReadAsync<TaskRepresentation>(secondResponse);
        second.Id.ShouldBe(first.Id);
        second.Status.ShouldBe(first.Status);
        Math.Abs((second.UpdatedAt - first.UpdatedAt).TotalMilliseconds).ShouldBeLessThan(1);
    }

    [Test]
    public async Task DeleteTask()
    {
        TaskRepresentation created = await CreateTaskAsync("Delete the reference task");

        using HttpResponseMessage deleteResponse = await Client.DeleteAsync($"{TasksPath}/{created.Id}");
        await deleteResponse.ShouldBeStatusAsync(HttpStatusCode.NoContent);

        using HttpResponseMessage getResponse = await Client.GetAsync($"{TasksPath}/{created.Id}");
        await getResponse.ShouldBeStatusAsync(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ValidationAndMalformedJsonReturnProblemDetails()
    {
        using HttpResponseMessage validationResponse = await Client.PostAsJsonAsync(
            TasksPath,
            NewTask("xx"));
        await validationResponse.ShouldBeStatusAsync(HttpStatusCode.BadRequest);
        validationResponse.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        ProblemResponse validationProblem =
            await IntegrationTestFixture.ReadAsync<ProblemResponse>(validationResponse);
        validationProblem.Type.ShouldNotBeNullOrWhiteSpace();

        using StringContent content = new("{", Encoding.UTF8, "application/json");
        using HttpResponseMessage jsonResponse = await Client.PostAsync(TasksPath, content);
        await jsonResponse.ShouldBeStatusAsync(HttpStatusCode.BadRequest);
        jsonResponse.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        ProblemResponse jsonProblem = await IntegrationTestFixture.ReadAsync<ProblemResponse>(jsonResponse);
        jsonProblem.Type.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task MissingTaskReturnsCorrelatedProblemDetails()
    {
        using HttpResponseMessage response = await Client.GetAsync($"{TasksPath}/{Guid.CreateVersion7()}");

        await response.ShouldBeStatusAsync(HttpStatusCode.NotFound);
        ProblemResponse problem = await IntegrationTestFixture.ReadAsync<ProblemResponse>(response);
        problem.Status.ShouldBe((int)HttpStatusCode.NotFound);
        problem.Type.ShouldBe(ServiceProblemTypeRoot + "task-not-found");
        problem.Code.ShouldBe("Task.NotFound");
        problem.TraceId.ShouldNotBeNullOrWhiteSpace();
        problem.RequestId.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task UnknownRouteReturnsCommonProblemDetails()
    {
        using HttpResponseMessage response = await Client.GetAsync("/does-not-exist");

        await response.ShouldBeStatusAsync(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        ProblemResponse problem = await IntegrationTestFixture.ReadAsync<ProblemResponse>(response);
        problem.Status.ShouldBe((int)HttpStatusCode.NotFound);
        problem.Type.ShouldBe(ProblemTypeRoot + "common/not-found");
        problem.Code.ShouldBe("Resource.NotFound");
        problem.RequestId.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task HealthAndAliveAreAvailable()
    {
        using HttpResponseMessage health = await Client.GetAsync("/health");
        using HttpResponseMessage alive = await Client.GetAsync("/alive");
        await health.ShouldBeStatusAsync(HttpStatusCode.OK);
        await alive.ShouldBeStatusAsync(HttpStatusCode.OK);
    }

    [Test]
    public async Task OpenApiAndScalarAreAvailableInDevelopment()
    {
        using HttpResponseMessage openApiResponse = await Client.GetAsync("/openapi/v1.json");
        await openApiResponse.ShouldBeStatusAsync(HttpStatusCode.OK);
        openApiResponse.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");

        await using Stream openApiContent = await openApiResponse.Content.ReadAsStreamAsync();
        using JsonDocument openApi = await JsonDocument.ParseAsync(openApiContent);
        openApi.RootElement
            .GetProperty("paths")
            .TryGetProperty(TasksPath, out _)
            .ShouldBeTrue();

        using HttpResponseMessage scalarResponse = await Client.GetAsync("/scalar/v1");
        await scalarResponse.ShouldBeStatusAsync(HttpStatusCode.OK);
        scalarResponse.Content.Headers.ContentType?.MediaType.ShouldBe("text/html");
    }

    private async Task<TaskRepresentation> CreateTaskAsync(string title)
    {
        CreateTaskRequest request = NewTask(title);
        using HttpResponseMessage response = await Client.PostAsJsonAsync(TasksPath, request);
        await response.ShouldBeStatusAsync(HttpStatusCode.Created);
        response.Headers.Location?.ToString().ShouldStartWith($"{TasksPath}/");

        TaskRepresentation task = await IntegrationTestFixture.ReadAsync<TaskRepresentation>(response);
        task.Id.ShouldNotBe(Guid.Empty);
        task.Title.ShouldBe(request.Title);
        task.Description.ShouldBe(request.Description);
        task.Status.ShouldBe(TaskItemStatus.Todo);
        return task;
    }

    private static CreateTaskRequest NewTask(string title) =>
        new(title, "A valid task description used by the integration test suite.");

    private sealed record ProblemResponse(
        int Status,
        string Title,
        string Detail,
        string Type,
        string Code,
        string TraceId,
        string RequestId);
}
