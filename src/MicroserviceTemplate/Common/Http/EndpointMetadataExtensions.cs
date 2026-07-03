namespace MicroserviceTemplate.Common.Http;

public static class EndpointMetadataExtensions
{
    public static RouteHandlerBuilder ProducesCommonProblems(this RouteHandlerBuilder builder) =>
        builder
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
}
