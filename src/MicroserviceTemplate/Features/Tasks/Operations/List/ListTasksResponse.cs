using TaskStatus = MicroserviceTemplate.Features.Tasks.Models.TaskStatus;

namespace MicroserviceTemplate.Features.Tasks.Operations.List;

public sealed record ListTasksResponse(
    Guid Id,
    string Title,
    string Description,
    TaskStatus Status,
    DateTimeOffset? DueDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
