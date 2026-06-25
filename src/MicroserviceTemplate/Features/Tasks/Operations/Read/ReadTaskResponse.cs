using TaskStatus = MicroserviceTemplate.Features.Tasks.Models.TaskStatus;

namespace MicroserviceTemplate.Features.Tasks.Operations.Read;

public sealed record ReadTaskResponse(
    Guid Id,
    string Title,
    string Description,
    TaskStatus Status,
    DateTimeOffset? DueDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
