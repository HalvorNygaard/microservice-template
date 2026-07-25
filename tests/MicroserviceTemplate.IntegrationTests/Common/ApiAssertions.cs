namespace ModernMicroservice.IntegrationTests.Common;

internal static class ApiAssertions
{
    internal static async Task ShouldBeStatusAsync(
        this HttpResponseMessage response,
        HttpStatusCode expected,
        CancellationToken cancellationToken = default)
    {
        if (response.StatusCode == expected)
        {
            return;
        }

        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        response.StatusCode.ShouldBe(expected, body);
    }
}
