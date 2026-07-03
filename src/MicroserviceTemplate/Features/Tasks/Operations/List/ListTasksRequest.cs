namespace MicroserviceTemplate.Features.Tasks.Operations.List;

public sealed record ListTasksRequest(int? PageNumber = null, int? PageSize = null)
{
    public const int DefaultPageNumber = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageNumber = 100_000;
    public const int MaxPageSize = 100;

    public int ResolvedPageNumber => PageNumber is null or < 1
        ? DefaultPageNumber
        : Math.Min(PageNumber.Value, MaxPageNumber);

    public int ResolvedPageSize => PageSize is null or < 1
        ? DefaultPageSize
        : Math.Min(PageSize.Value, MaxPageSize);
}
