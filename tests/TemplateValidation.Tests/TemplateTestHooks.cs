namespace TemplateValidation.Tests;

public static class TemplateTestHooks
{
    [After(TestSession)]
    public static async Task Cleanup()
    {
        await TemplateValidationTests.RunDotNetCommand(
            TemplateValidationTests.RepoRoot,
            "new",
            "uninstall",
            TemplateValidationTests.TemplateIdentity);

        if (Directory.Exists(TemplateValidationTests.DistPath))
        {
            try
            {
                Directory.Delete(TemplateValidationTests.DistPath, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
