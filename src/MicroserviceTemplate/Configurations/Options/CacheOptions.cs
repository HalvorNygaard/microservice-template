using System.ComponentModel.DataAnnotations;

namespace MicroserviceTemplate.Configurations.Options;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    [Range(1, 86_400)]
    public int AbsoluteExpirationSeconds { get; init; } = 300;

    [Range(1, 86_400)]
    public int SlidingExpirationSeconds { get; init; } = 120;
}
