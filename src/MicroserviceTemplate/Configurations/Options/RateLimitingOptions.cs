using System.ComponentModel.DataAnnotations;

namespace MicroserviceTemplate.Configurations.Options;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting:Api";

    [Range(1, int.MaxValue)]
    public int PermitLimit { get; init; } = 100;

    [Range(1, int.MaxValue)]
    public int WindowSeconds { get; init; } = 60;

    [Range(0, int.MaxValue)]
    public int QueueLimit { get; init; }
}
