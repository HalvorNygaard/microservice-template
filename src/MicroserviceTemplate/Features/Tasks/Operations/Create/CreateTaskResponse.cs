using TaskStatus = MicroserviceTemplate.Features.Tasks.Models.TaskStatus;

namespace MicroserviceTemplate.Features.Tasks.Operations.Create;

public sealed record CreateTaskResponse(
    Guid Id,
    string Title,
    string Description,
    TaskStatus Status,
    DateTimeOffset? DueDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
