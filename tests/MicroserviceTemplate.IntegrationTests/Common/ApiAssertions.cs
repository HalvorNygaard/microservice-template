namespace MicroserviceTemplate.Tests.Common;

public static class ApiAssertions
{
    public static async Task ShouldBeWithBodyAsync(
        this HttpStatusCode actual,
        HttpStatusCode expected,
        HttpResponseMessage response)
    {
        if (actual == expected)
        {
            return;
        }

        string body = await response.Content.ReadAsStringAsync();
        actual.ShouldBe(expected, body);
    }
}
