using TaskStatus = MicroserviceTemplate.Features.Tasks.Models.TaskStatus;

namespace MicroserviceTemplate.Features.Tasks.Operations.Update;

public sealed record UpdateTaskResponse(
    Guid Id,
    string Title,
    string Description,
    TaskStatus Status,
    DateTimeOffset? DueDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
