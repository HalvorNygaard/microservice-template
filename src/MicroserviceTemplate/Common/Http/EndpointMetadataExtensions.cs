namespace ModernMicroservice.Common.Http;

internal static class EndpointMetadataExtensions
{
    internal static RouteHandlerBuilder ProducesCommonProblems(this RouteHandlerBuilder builder) =>
        builder
            .ProducesProblem(StatusCodes.Status504GatewayTimeout)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
}
