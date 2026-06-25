using System.ComponentModel.DataAnnotations;
using TaskStatus = MicroserviceTemplate.Features.Tasks.Models.TaskStatus;

namespace MicroserviceTemplate.Features.Tasks.Operations.Create;

public sealed record CreateTaskRequest(
    [property: Required(ErrorMessage = "Title is required")]
    [property: StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 200 characters")]
    string Title,

    [property: Required(ErrorMessage = "Description is required")]
    [property: StringLength(2000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 2000 characters")]
    string Description,

    [property: EnumDataType(typeof(TaskStatus), ErrorMessage = "Invalid status value")]
    TaskStatus Status = TaskStatus.Todo,
    DateTimeOffset? DueDate = null);
