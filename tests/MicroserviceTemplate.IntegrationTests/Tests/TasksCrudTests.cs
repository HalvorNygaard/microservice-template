using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MicroserviceTemplate.Features.Tasks.Operations.Create;
using MicroserviceTemplate.Features.Tasks.Operations.List;
using MicroserviceTemplate.Features.Tasks.Operations.Read;
using MicroserviceTemplate.Features.Tasks.Operations.Update;
using MicroserviceTemplate.Tests.Common;
using TaskStatus = MicroserviceTemplate.Features.Tasks.Models.TaskStatus;

namespace MicroserviceTemplate.Tests;

[ClassDataSource<IntegrationTestFixture>(Shared = SharedType.PerTestSession)]
public class TasksCrudTests(IntegrationTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private HttpClient HttpClient => fixture.Client;

    [Test]
    public async Task GetAllTasks_WhenRequested_ReturnsTasks()
    {
        var response = await HttpClient.GetAsync("/api/tasks");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var tasks = await response.Content.ReadFromJsonAsync<List<ListTasksResponse>>(JsonOptions);
        tasks.ShouldNotBeNull();
        tasks.ShouldNotBeEmpty();
        tasks[0].Title.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task CreateTask_WhenValidRequest_ReturnsCreatedTask()
    {
        var request = CreateTaskRequest();

        var response = await HttpClient.PostAsJsonAsync("/api/tasks", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location?.ToString().ShouldStartWith("/api/tasks/");

        var created = await response.Content.ReadFromJsonAsync<CreateTaskResponse>(JsonOptions);
        AssertCreatedTask(created, request);
    }

    [Test]
    public async Task GetTaskById_WhenKnownId_ReturnsTask()
    {
        var request = CreateTaskRequest("Get by id test", TaskStatus.InProgress, DateTimeOffset.UtcNow.AddDays(2));
        var created = await CreateTaskAsync(request);

        var response = await HttpClient.GetAsync($"/api/tasks/{created.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var task = await response.Content.ReadFromJsonAsync<ReadTaskResponse>(JsonOptions);
        AssertTask(task, request, created.Id);
    }

    [Test]
    public async Task UpdateTask_WhenKnownId_ReturnsUpdatedTask()
    {
        var createRequest = CreateTaskRequest("Update test", TaskStatus.Todo, DateTimeOffset.UtcNow.AddDays(5));
        var created = await CreateTaskAsync(createRequest);
        var updateRequest = new UpdateTaskRequest(
            "Updated title",
            "Updated description",
            TaskStatus.Done,
            DateTimeOffset.UtcNow.AddDays(1));

        var response = await HttpClient.PutAsJsonAsync($"/api/tasks/{created.Id}", updateRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<UpdateTaskResponse>(JsonOptions);
        updated.ShouldNotBeNull();
        updated.Id.ShouldBe(created.Id);
        updated.Title.ShouldBe(updateRequest.Title);
        updated.Description.ShouldBe(updateRequest.Description);
        updated.Status.ShouldBe(updateRequest.Status);
        updated.DueDate.ShouldBe(updateRequest.DueDate);
        Math.Abs((updated.CreatedAt - created.CreatedAt).TotalMilliseconds).ShouldBeLessThan(1000);
        updated.UpdatedAt.ShouldNotBe(created.UpdatedAt);
    }

    [Test]
    public async Task DeleteTask_WhenKnownId_RemovesTask()
    {
        var request = CreateTaskRequest("Delete test", TaskStatus.Todo, null);
        var created = await CreateTaskAsync(request);

        var deleteResponse = await HttpClient.DeleteAsync($"/api/tasks/{created.Id}");

        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResponse = await HttpClient.GetAsync($"/api/tasks/{created.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CreateTask_WhenTitleIsTooShort_ReturnsBadRequest()
    {
        var request = CreateTaskRequest(title: "xx");

        var response = await HttpClient.PostAsJsonAsync("/api/tasks", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateTask_WhenJsonIsInvalid_ReturnsBadRequest()
    {
        using var content = new StringContent("{", Encoding.UTF8, "application/json");

        var response = await HttpClient.PostAsync("/api/tasks", content);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task HealthAndAlive_WhenRequested_ReturnOk()
    {
        var healthResponse = await HttpClient.GetAsync("/health");
        var aliveResponse = await HttpClient.GetAsync("/alive");

        healthResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        aliveResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetTaskById_WhenIdIsNotGuid_ReturnsBadRequest()
    {
        var response = await HttpClient.GetAsync("/api/tasks/not-a-guid");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Test]
    public async Task GetTaskById_WhenIdDoesNotExist_ReturnsNotFound()
    {
        var response = await HttpClient.GetAsync($"/api/tasks/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<CreateTaskResponse> CreateTaskAsync(CreateTaskRequest request)
    {
        var response = await HttpClient.PostAsJsonAsync("/api/tasks", request);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<CreateTaskResponse>(JsonOptions);
        AssertCreatedTask(created, request);

        return created!;
    }

    private static CreateTaskRequest CreateTaskRequest(
        string title = "Test task",
        TaskStatus status = TaskStatus.Todo,
        DateTimeOffset? dueDate = null)
    {
        return new CreateTaskRequest(
            title,
            "Valid task description for integration tests",
            status,
            dueDate ?? DateTimeOffset.UtcNow.AddDays(3));
    }

    private static void AssertCreatedTask(CreateTaskResponse? created, CreateTaskRequest request)
    {
        created.ShouldNotBeNull();
        created.Id.ShouldNotBe(Guid.Empty);
        AssertTask(created, request, created.Id);
    }

    private static void AssertTask(ReadTaskResponse? task, CreateTaskRequest request, Guid id)
    {
        task.ShouldNotBeNull();
        task.Id.ShouldBe(id);
        task.Title.ShouldBe(request.Title);
        task.Description.ShouldBe(request.Description);
        task.Status.ShouldBe(request.Status);
        Math.Abs((task.DueDate!.Value - request.DueDate!.Value).TotalMilliseconds).ShouldBeLessThan(1000);
        task.CreatedAt.ShouldBeGreaterThan(DateTimeOffset.MinValue.AddDays(1));
        task.UpdatedAt.ShouldBeGreaterThan(DateTimeOffset.MinValue.AddDays(1));
    }

    private static void AssertTask(CreateTaskResponse? task, CreateTaskRequest request, Guid id)
    {
        task.ShouldNotBeNull();
        task.Id.ShouldBe(id);
        task.Title.ShouldBe(request.Title);
        task.Description.ShouldBe(request.Description);
        task.Status.ShouldBe(request.Status);
        Math.Abs((task.DueDate!.Value - request.DueDate!.Value).TotalMilliseconds).ShouldBeLessThan(1000);
        task.CreatedAt.ShouldBeGreaterThan(DateTimeOffset.MinValue.AddDays(1));
        task.UpdatedAt.ShouldBeGreaterThan(DateTimeOffset.MinValue.AddDays(1));
    }
}
