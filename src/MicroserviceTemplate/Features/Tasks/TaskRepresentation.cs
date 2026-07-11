using System.Linq.Expressions;

namespace MicroserviceTemplate.Features.Tasks;

public sealed record TaskRepresentation(
    Guid Id,
    string Title,
    string Description,
    TaskItemStatus Status,
    DateTimeOffset? DueDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    Guid Version);

internal static class TaskMappings
{
    internal static readonly Expression<Func<TaskItem, TaskRepresentation>> Projection = task => new(
        task.Id,
        task.Title,
        task.Description,
        task.Status,
        task.DueDate,
        task.CreatedAt,
        task.UpdatedAt,
        task.Version);

    internal static TaskRepresentation ToRepresentation(this TaskItem task) => new(
        task.Id,
        task.Title,
        task.Description,
        task.Status,
        task.DueDate,
        task.CreatedAt,
        task.UpdatedAt,
        task.Version);
}
