using System.Text;
using MicroserviceTemplate.Common.Http;
using MicroserviceTemplate.Features.Tasks;
using MicroserviceTemplate.Features.Tasks.Complete;
using MicroserviceTemplate.Features.Tasks.Create;
using MicroserviceTemplate.Features.Tasks.List;
using MicroserviceTemplate.Features.Tasks.Update;
using MicroserviceTemplate.Tests.Common;

namespace MicroserviceTemplate.Tests;

[ClassDataSource<IntegrationTestFixture>(Shared = SharedType.PerTestSession)]
public sealed class TasksApiTests(IntegrationTestFixture fixture)
{
    private const string TasksPath = "/api/v1/tasks";
    private HttpClient Client => fixture.Client;

    [Test]
    public async Task Create_Get_And_List_Task()
    {
        CreateTask.Request request = NewTask("Read the architecture guide");
        TaskRepresentation created = await CreateTaskAsync(request);

        using HttpResponseMessage getResponse = await Client.GetAsync($"{TasksPath}/{created.Id}");
        await getResponse.ShouldBeStatusAsync(HttpStatusCode.OK);
        TaskRepresentation read = await IntegrationTestFixture.ReadAsync<TaskRepresentation>(getResponse);
        read.Id.ShouldBe(created.Id);
        read.Title.ShouldBe(created.Title);
        read.Description.ShouldBe(created.Description);
        read.Status.ShouldBe(created.Status);
        read.Version.ShouldBe(created.Version);
        Math.Abs((read.CreatedAt - created.CreatedAt).TotalMilliseconds).ShouldBeLessThan(1);
        Math.Abs((read.UpdatedAt - created.UpdatedAt).TotalMilliseconds).ShouldBeLessThan(1);

        using HttpResponseMessage listResponse = await Client.GetAsync($"{TasksPath}?pageSize=100");
        await listResponse.ShouldBeStatusAsync(HttpStatusCode.OK);
        PagedResult<TaskRepresentation> page = await IntegrationTestFixture.ReadAsync<PagedResult<TaskRepresentation>>(listResponse);
        page.Items.ShouldContain(task => task.Id == created.Id);
        page.Items.Select(task => task.CreatedAt).ShouldBeInOrder(SortDirection.Descending);
    }

    [Test]
    public async Task List_Clamps_Page_Inputs()
    {
        using HttpResponseMessage response = await Client.GetAsync(
            $"{TasksPath}?pageNumber={int.MaxValue}&pageSize={int.MaxValue}");

        await response.ShouldBeStatusAsync(HttpStatusCode.OK);
        PagedResult<TaskRepresentation> page = await IntegrationTestFixture.ReadAsync<PagedResult<TaskRepresentation>>(response);
        page.PageNumber.ShouldBe(ListTasks.MaxPageNumber);
        page.PageSize.ShouldBe(ListTasks.MaxPageSize);
    }

    [Test]
    public async Task Update_Uses_Optimistic_Concurrency()
    {
        TaskRepresentation created = await CreateTaskAsync(NewTask("Concurrent update"));
        UpdateTask.Request firstRequest = Update(created, "First writer", TaskItemStatus.InProgress);
        UpdateTask.Request secondRequest = Update(created, "Second writer", TaskItemStatus.InProgress);

        Task<HttpResponseMessage> first = Client.PutAsJsonAsync($"{TasksPath}/{created.Id}", firstRequest);
        Task<HttpResponseMessage> second = Client.PutAsJsonAsync($"{TasksPath}/{created.Id}", secondRequest);
        HttpResponseMessage[] responses = await Task.WhenAll(first, second);
        using HttpResponseMessage firstResponse = responses[0];
        using HttpResponseMessage secondResponse = responses[1];

        responses.Count(response => response.StatusCode == HttpStatusCode.OK).ShouldBe(1);
        responses.Count(response => response.StatusCode == HttpStatusCode.Conflict).ShouldBe(1);

        HttpResponseMessage conflict = responses.Single(response => response.StatusCode == HttpStatusCode.Conflict);
        ProblemResponse problem = await IntegrationTestFixture.ReadAsync<ProblemResponse>(conflict);
        problem.ErrorCode.ShouldBe("Task.VersionConflict");
        problem.TraceId.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Complete_Changes_Status_And_Version()
    {
        TaskRepresentation created = await CreateTaskAsync(NewTask("Complete operation"));

        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            $"{TasksPath}/{created.Id}/complete",
            new CompleteTask.Request(created.Version));

        await response.ShouldBeStatusAsync(HttpStatusCode.OK);
        TaskRepresentation completed = await IntegrationTestFixture.ReadAsync<TaskRepresentation>(response);
        completed.Status.ShouldBe(TaskItemStatus.Done);
        completed.Version.ShouldNotBe(created.Version);
        completed.UpdatedAt.ShouldBeGreaterThanOrEqualTo(created.UpdatedAt);
    }

    [Test]
    public async Task Cancelled_Task_Cannot_Be_Completed()
    {
        TaskRepresentation created = await CreateTaskAsync(NewTask("Cancelled transition"));
        using HttpResponseMessage updateResponse = await Client.PutAsJsonAsync(
            $"{TasksPath}/{created.Id}",
            Update(created, "Cancelled transition", TaskItemStatus.Cancelled));
        await updateResponse.ShouldBeStatusAsync(HttpStatusCode.OK);
        TaskRepresentation cancelled = await IntegrationTestFixture.ReadAsync<TaskRepresentation>(updateResponse);

        using HttpResponseMessage completeResponse = await Client.PostAsJsonAsync(
            $"{TasksPath}/{created.Id}/complete",
            new CompleteTask.Request(cancelled.Version));

        await completeResponse.ShouldBeStatusAsync(HttpStatusCode.Conflict);
        ProblemResponse problem = await IntegrationTestFixture.ReadAsync<ProblemResponse>(completeResponse);
        problem.ErrorCode.ShouldBe("Task.InvalidTransition");
    }

    [Test]
    public async Task Delete_Removes_Task()
    {
        TaskRepresentation created = await CreateTaskAsync(NewTask("Delete operation"));

        using HttpResponseMessage staleDeleteResponse = await Client.DeleteAsync(
            $"{TasksPath}/{created.Id}?version={Guid.CreateVersion7()}");
        await staleDeleteResponse.ShouldBeStatusAsync(HttpStatusCode.Conflict);

        using HttpResponseMessage deleteResponse = await Client.DeleteAsync(
            $"{TasksPath}/{created.Id}?version={created.Version}");
        await deleteResponse.ShouldBeStatusAsync(HttpStatusCode.NoContent);

        using HttpResponseMessage getResponse = await Client.GetAsync($"{TasksPath}/{created.Id}");
        await getResponse.ShouldBeStatusAsync(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Validation_And_Malformed_Json_Return_Problem_Details()
    {
        using HttpResponseMessage validationResponse = await Client.PostAsJsonAsync(
            TasksPath,
            NewTask("xx"));
        await validationResponse.ShouldBeStatusAsync(HttpStatusCode.BadRequest);
        validationResponse.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        using StringContent content = new("{", Encoding.UTF8, "application/json");
        using HttpResponseMessage jsonResponse = await Client.PostAsync(TasksPath, content);
        await jsonResponse.ShouldBeStatusAsync(HttpStatusCode.BadRequest);
        jsonResponse.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Test]
    public async Task Validation_Rejects_Values_That_Are_Too_Short_After_Trimming()
    {
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            TasksPath,
            new CreateTask.Request("  a  ", "  short  ", null));

        await response.ShouldBeStatusAsync(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Test]
    public async Task Missing_Task_Returns_Correlated_Problem_Details()
    {
        using HttpResponseMessage response = await Client.GetAsync($"{TasksPath}/{Guid.CreateVersion7()}");

        await response.ShouldBeStatusAsync(HttpStatusCode.NotFound);
        ProblemResponse problem = await IntegrationTestFixture.ReadAsync<ProblemResponse>(response);
        problem.Status.ShouldBe((int)HttpStatusCode.NotFound);
        problem.ErrorCode.ShouldBe("Task.NotFound");
        problem.TraceId.ShouldNotBeNullOrWhiteSpace();
        problem.RequestId.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Invalid_Route_Id_Does_Not_Match_Task_Endpoint()
    {
        using HttpResponseMessage response = await Client.GetAsync($"{TasksPath}/not-a-guid");
        await response.ShouldBeStatusAsync(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Test]
    public async Task Health_And_Alive_Are_Available()
    {
        using HttpResponseMessage health = await Client.GetAsync("/health");
        using HttpResponseMessage alive = await Client.GetAsync("/alive");
        await health.ShouldBeStatusAsync(HttpStatusCode.OK);
        await alive.ShouldBeStatusAsync(HttpStatusCode.OK);
    }

    private async Task<TaskRepresentation> CreateTaskAsync(CreateTask.Request request)
    {
        using HttpResponseMessage response = await Client.PostAsJsonAsync(TasksPath, request);
        await response.ShouldBeStatusAsync(HttpStatusCode.Created);
        response.Headers.Location?.ToString().ShouldStartWith($"{TasksPath}/");
        TaskRepresentation task = await IntegrationTestFixture.ReadAsync<TaskRepresentation>(response);
        task.Id.ShouldNotBe(Guid.Empty);
        task.Version.ShouldNotBe(Guid.Empty);
        task.Title.ShouldBe(request.Title);
        task.Description.ShouldBe(request.Description);
        task.Status.ShouldBe(TaskItemStatus.Todo);
        return task;
    }

    private static CreateTask.Request NewTask(string title) =>
        new(title, "A valid task description used by the integration test suite.", DateTimeOffset.UtcNow.AddDays(3));

    private static UpdateTask.Request Update(
        TaskRepresentation task,
        string title,
        TaskItemStatus status) =>
        new(title, task.Description, status, task.DueDate, task.Version);

    private sealed record ProblemResponse(
        int Status,
        string Title,
        string Detail,
        string ErrorCode,
        string TraceId,
        string RequestId);
}
