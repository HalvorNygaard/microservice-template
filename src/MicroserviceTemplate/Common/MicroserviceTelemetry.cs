using System.Diagnostics.Metrics;

namespace ModernMicroservice.Common;

internal static class MicroserviceTelemetry
{
    internal const string MeterName = "ModernMicroservice";
    private static readonly string AttributePrefix = MeterName.ToLowerInvariant();
    private static readonly string OperationAttributeName = Name("operation");
    internal static readonly string StatusCodeAttributeName = Name("status_code");
    internal static readonly string ErrorCategoryAttributeName = Name("error_category");

    internal static readonly Meter Meter = new(MeterName);

    internal static string Name(string name) => $"{AttributePrefix}.{name}";

    internal static KeyValuePair<string, object?> OperationTag(string operation) =>
        new(OperationAttributeName, operation);

    internal static KeyValuePair<string, object?> StatusCodeTag(int statusCode) =>
        new(StatusCodeAttributeName, statusCode);

    internal static KeyValuePair<string, object?> ErrorCategoryTag(string errorCategory) =>
        new(ErrorCategoryAttributeName, errorCategory);
}
