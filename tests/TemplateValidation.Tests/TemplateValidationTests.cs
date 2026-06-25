using System.Diagnostics;

namespace TemplateValidation.Tests;

/// <summary>
/// Template validation tests that verify the dotnet new template works correctly.
///
/// IMPORTANT: These tests should be run AFTER the integration tests pass:
///   1. dotnet test --project tests/MicroserviceTemplate.IntegrationTests/MicroserviceTemplate.IntegrationTests.csproj
///   2. dotnet test --project tests/TemplateValidation.Tests/TemplateValidation.Tests.csproj
///
/// These tests spawn dotnet processes which can conflict with parallel test execution.
/// Run with --parallel disabled or ensure no other dotnet test processes are running.
/// </summary>
public class TemplateValidationTests
{
    internal const string TemplateIdentity = "ModernMicroservice.Template";
    internal const string TemplateShortName = "modern-microservice";

    internal static readonly string RepoRoot = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..");
    internal static readonly string DistPath = Path.Combine(RepoRoot, "dist");

    private static readonly string TemplateProjectPath = Path.Combine(RepoRoot, "template");
    private static readonly string NupkgPath = Path.Combine(RepoRoot, "template", "bin", "Release");
    private static readonly TimeSpan DefaultCommandTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan GeneratedTestTimeout = TimeSpan.FromMinutes(10);
    private static readonly string[] ExpectedTopLevelEntries =
    [
        ".editorconfig",
        ".gitattributes",
        ".gitignore",
        "Directory.Build.props",
        "Directory.Packages.props",
        "LICENSE",
        $"{TestServiceName}.slnx",
        "NuGet.Config",
        "README.md",
        "global.json",
        "src",
        "tests"
    ];
    private static readonly string[] ForbiddenGeneratedPaths =
    [
        ".github",
        ".aspire",
        "dist",
        "template",
        "tests/TemplateValidation.Tests",
        "vision.md"
    ];
    private static readonly string[] ForbiddenGeneratedFragments =
    [
        "MicroserviceTemplate",
        "<ServiceName>",
        "microservice-template",
        "Cillco",
        "cillco",
        "cpurch",
        "legacy-c-purch",
        "Halvor",
        "halvo",
        "Your Organization",
        "TemplateValidation.Tests"
    ];
    private static readonly string[] BinaryExtensions =
    [
        ".dll",
        ".exe",
        ".pdb",
        ".png",
        ".jpg",
        ".jpeg",
        ".gif",
        ".nupkg",
        ".snupkg"
    ];

    private const string TestServiceName = "MyAwesomeService";
    private const string TestServiceKebabName = "my-awesome-service";
    private const string TestOutputDirectoryName = "my-awesome-service";

    [Test]
    public async Task Template_Generates_Clean_Buildable_Service()
    {
        EnsureDistIsEmpty();

        try
        {
            await BuildAndInstallTemplateAsync();

            var outputPath = await GenerateProjectAsync();

            AssertGeneratedProjectTree(outputPath);
            AssertGeneratedMigrationsExist(outputPath);
            AssertGeneratedProjectFilesExist(outputPath);
            AssertGeneratedSolution(outputPath);
            AssertGeneratedProjectReferences(outputPath);
            AssertGeneratedAppHost(outputPath);
            AssertGeneratedLaunchSettings(outputPath);
            await AssertGeneratedTextDoesNotContainForbiddenFragments(outputPath);
            AssertGeneratedReadme(outputPath);

            await AssertGeneratedProjectBuildsAsync(outputPath);
            await AssertGeneratedIntegrationTestsPassAsync(outputPath);
        }
        finally
        {
            await UninstallTemplateAsync();
            CleanupDist();
        }
    }

    internal static async Task<CommandResult> RunDotNetCommand(string workingDirectory, params string[] arguments)
    {
        return await RunDotNetCommand(DefaultCommandTimeout, workingDirectory, arguments);
    }

    private static async Task<string> GenerateProjectAsync()
    {
        var outputPath = Path.Combine(DistPath, TestOutputDirectoryName);
        var createResult = await RunDotNetCommand(
            RepoRoot,
            "new",
            TemplateShortName,
            "--name",
            TestServiceName,
            "--output",
            outputPath);

        createResult.ExitCode.ShouldBe(0, $"Template creation failed: {createResult.Output}\n{createResult.Error}");
        Directory.Exists(outputPath).ShouldBeTrue($"Expected generated project at {outputPath}.");

        return outputPath;
    }

    private static void AssertGeneratedProjectTree(string outputPath)
    {
        Directory.Exists(outputPath).ShouldBeTrue();

        var actualTopLevelEntries = Directory.GetFileSystemEntries(outputPath)
            .Select(Path.GetFileName)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        actualTopLevelEntries.ShouldBe(ExpectedTopLevelEntries, "Generated project has unexpected top-level entries.");

        foreach (var forbiddenPath in ForbiddenGeneratedPaths)
        {
            var path = Path.Combine(outputPath, forbiddenPath);
            (Directory.Exists(path) || File.Exists(path)).ShouldBeFalse(
                $"Generated project should not contain template-only path: {forbiddenPath}");
        }
    }

    private static void AssertGeneratedMigrationsExist(string outputPath)
    {
        var migrationsPath = Path.Combine(outputPath, "src", TestServiceName, "Infrastructure", "Data", "Migrations");
        var migrationFiles = Directory.GetFiles(migrationsPath, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .ToArray();

        migrationFiles.ShouldContain("20260119195356_Initial.cs");
        migrationFiles.ShouldContain("20260119195356_Initial.Designer.cs");
        migrationFiles.ShouldContain("ApplicationDbContextModelSnapshot.cs");
    }

    private static void AssertGeneratedProjectFilesExist(string outputPath)
    {
        var filesToCheck = new[]
        {
            Path.Combine(outputPath, TestServiceName + ".slnx"),
            Path.Combine(outputPath, "src", TestServiceName, $"{TestServiceName}.csproj"),
            Path.Combine(outputPath, "src", TestServiceName, "Program.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Configurations", "Options", "CacheOptions.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Configurations", "Options", "RateLimitingOptions.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Configurations", "Setup", "MicroserviceSetup.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Configurations", "Setup", "RateLimitingSetup.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Features", "Tasks", "TaskFeature.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Features", "Tasks", "Models", "TaskItem.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Features", "Tasks", "Models", "TaskItemConfiguration.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Features", "Tasks", "Operations", "Create", "CreateTaskHandler.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Features", "Tasks", "Operations", "Create", "CreateTaskRequest.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Features", "Tasks", "Operations", "Create", "CreateTaskResponse.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Features", "Tasks", "Operations", "Delete", "DeleteTaskHandler.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Features", "Tasks", "Operations", "Delete", "DeleteTaskRequest.cs"),
            Path.Combine(outputPath, "src", $"{TestServiceName}.AppHost", $"{TestServiceName}.AppHost.csproj"),
            Path.Combine(outputPath, "src", $"{TestServiceName}.AppHost", "AppHost.cs"),
            Path.Combine(outputPath, "src", $"{TestServiceName}.AppHost", "Properties", "launchSettings.json"),
            Path.Combine(outputPath, "tests", $"{TestServiceName}.IntegrationTests", $"{TestServiceName}.IntegrationTests.csproj"),
            Path.Combine(outputPath, "tests", $"{TestServiceName}.IntegrationTests", "Common", "TestFixture.cs"),
            Path.Combine(outputPath, "tests", $"{TestServiceName}.IntegrationTests", "Tests", "TasksCrudTests.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Infrastructure", "Data", "Migrations", "20260119195356_Initial.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Infrastructure", "Data", "Migrations", "20260119195356_Initial.Designer.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Infrastructure", "Data", "Migrations", "ApplicationDbContextModelSnapshot.cs")
        };

        foreach (var file in filesToCheck)
        {
            File.Exists(file).ShouldBeTrue($"Expected generated file: {file}");
        }
    }

    private static void AssertGeneratedSolution(string outputPath)
    {
        var slnFile = Path.Combine(outputPath, $"{TestServiceName}.slnx");
        var slnContent = ReadTextFile(slnFile);

        slnContent.ShouldContain($"<Project Path=\"src/{TestServiceName}/{TestServiceName}.csproj\" />");
        slnContent.ShouldContain($"<Project Path=\"src/{TestServiceName}.AppHost/{TestServiceName}.AppHost.csproj\" />");
        slnContent.ShouldContain($"<Project Path=\"tests/{TestServiceName}.IntegrationTests/{TestServiceName}.IntegrationTests.csproj\" />");
        slnContent.ShouldNotContain("TemplateValidation.Tests");
        slnContent.ShouldNotContain("MicroserviceTemplate");
        slnContent.ShouldNotContain("microservice-template");
    }

    private static void AssertGeneratedProjectReferences(string outputPath)
    {
        var serviceProject = ReadTextFile(Path.Combine(outputPath, "src", TestServiceName, $"{TestServiceName}.csproj"));
        var appHostProject = ReadTextFile(Path.Combine(outputPath, "src", $"{TestServiceName}.AppHost", $"{TestServiceName}.AppHost.csproj"));
        var integrationProject = ReadTextFile(Path.Combine(outputPath, "tests", $"{TestServiceName}.IntegrationTests", $"{TestServiceName}.IntegrationTests.csproj"));

        serviceProject.ShouldContain("<RootNamespace>MyAwesomeService</RootNamespace>");
        serviceProject.ShouldContain("<TargetFramework>net10.0</TargetFramework>");

        appHostProject.ShouldContain($"<ProjectReference Include=\"..\\{TestServiceName}\\{TestServiceName}.csproj\" />");
        integrationProject.ShouldContain($"<ProjectReference Include=\"..\\..\\src\\{TestServiceName}\\{TestServiceName}.csproj\" />");
        integrationProject.ShouldContain($"<ProjectReference Include=\"..\\..\\src\\{TestServiceName}.AppHost\\{TestServiceName}.AppHost.csproj\" />");
    }

    private static void AssertGeneratedAppHost(string outputPath)
    {
        var appHost = ReadTextFile(Path.Combine(outputPath, "src", $"{TestServiceName}.AppHost", "AppHost.cs"));

        appHost.ShouldContain($"builder.AddProject<Projects.{TestServiceName}>(\"{TestServiceKebabName}\")");
        appHost.ShouldContain(".WithReference(cache).WaitFor(cache)");
        appHost.ShouldContain(".WithReference(postgresdb).WaitFor(postgresdb)");
        appHost.ShouldContain(".WithHttpHealthCheck(\"/health\")");
    }

    private static void AssertGeneratedLaunchSettings(string outputPath)
    {
        var launchSettings = ReadTextFile(Path.Combine(outputPath, "src", $"{TestServiceName}.AppHost", "Properties", "launchSettings.json"));

        launchSettings.ShouldContain($"{TestServiceKebabName}.dev.localhost");
        launchSettings.ShouldNotContain("template.dev.localhost");
        launchSettings.ShouldNotContain("ASPIRE_DASHBOARD_MCP_ENDPOINT_URL");
    }

    private static void AssertGeneratedReadme(string outputPath)
    {
        var readme = ReadTextFile(Path.Combine(outputPath, "README.md"));

        readme.ShouldContain($"# {TestServiceName}");
        readme.ShouldContain($"src/{TestServiceName}/{TestServiceName}.csproj");
        readme.ShouldContain($"src/{TestServiceName}.AppHost/{TestServiceName}.AppHost.csproj");
        readme.ShouldNotContain("<ServiceName>");
    }

    private static async Task AssertGeneratedTextDoesNotContainForbiddenFragments(string outputPath)
    {
        var violations = new List<string>();

        foreach (var file in EnumerateTextFiles(outputPath))
        {
            var content = await File.ReadAllTextAsync(file);
            var relativePath = Path.GetRelativePath(outputPath, file);

            foreach (var forbiddenFragment in ForbiddenGeneratedFragments)
            {
                if (content.Contains(forbiddenFragment, StringComparison.Ordinal))
                {
                    violations.Add($"{relativePath}: {forbiddenFragment}");
                }
            }
        }

        violations.ShouldBeEmpty(
            "Generated project contains forbidden template fragments:\n" +
            string.Join(Environment.NewLine, violations.Take(20)));
    }

    private static async Task AssertGeneratedProjectBuildsAsync(string outputPath)
    {
        var slnFile = Path.Combine(outputPath, $"{TestServiceName}.slnx");
        var buildResult = await RunDotNetCommand(
            GeneratedTestTimeout,
            outputPath,
            "build",
            slnFile);

        buildResult.ExitCode.ShouldBe(0, $"Build failed with output: {buildResult.Output}\nErrors: {buildResult.Error}");
    }

    private static async Task AssertGeneratedIntegrationTestsPassAsync(string outputPath)
    {
        var integrationTestsProject = Path.Combine(
            outputPath,
            "tests",
            $"{TestServiceName}.IntegrationTests",
            $"{TestServiceName}.IntegrationTests.csproj");

        var integrationTestResult = await RunDotNetCommand(
            GeneratedTestTimeout,
            outputPath,
            "test",
            "--project",
            integrationTestsProject);

        integrationTestResult.ExitCode.ShouldBe(0, $"Integration tests failed: {integrationTestResult.Output}\n{integrationTestResult.Error}");
    }

    private static async Task BuildAndInstallTemplateAsync()
    {
        CleanupTemplatePackages();

        var packResult = await RunDotNetCommand(
            RepoRoot,
            "pack",
            TemplateProjectPath,
            "-c",
            "Release");

        packResult.ExitCode.ShouldBe(0, $"Pack failed: {packResult.Output}\n{packResult.Error}");

        var nupkgFiles = Directory.GetFiles(NupkgPath, $"{TemplateIdentity}.*.nupkg")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ToArray();

        nupkgFiles.Length.ShouldBe(1, $"Expected exactly one {TemplateIdentity} package in {NupkgPath}.");

        await RunDotNetCommand(RepoRoot, "new", "uninstall", TemplateIdentity);

        var installResult = await RunDotNetCommand(RepoRoot, "new", "install", nupkgFiles[0]);
        installResult.ExitCode.ShouldBe(0, $"Install failed: {installResult.Output}\n{installResult.Error}");
        installResult.Output.ShouldContain(TemplateIdentity);
    }

    private static async Task UninstallTemplateAsync()
    {
        await RunDotNetCommand(RepoRoot, "new", "uninstall", TemplateIdentity);
    }

    private static void CleanupTemplatePackages()
    {
        if (!Directory.Exists(NupkgPath))
        {
            return;
        }

        foreach (var package in Directory.GetFiles(NupkgPath, $"{TemplateIdentity}.*.nupkg"))
        {
            File.Delete(package);
        }
    }

    private static void EnsureDistIsEmpty()
    {
        if (Directory.Exists(DistPath))
        {
            Directory.Delete(DistPath, recursive: true);
        }

        Directory.CreateDirectory(DistPath);
    }

    private static void CleanupDist()
    {
        if (!Directory.Exists(DistPath))
        {
            return;
        }

        try
        {
            Directory.Delete(DistPath, recursive: true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private static IEnumerable<string> EnumerateTextFiles(string rootPath)
    {
        foreach (var file in Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories))
        {
            if (IsExcludedFromTextScan(file) || IsBinaryFile(file))
            {
                continue;
            }

            yield return file;
        }
    }

    private static bool IsExcludedFromTextScan(string path)
    {
        var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Any(static part => part is "bin" or "obj" or ".git");
    }

    private static bool IsBinaryFile(string path)
    {
        return BinaryExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
    }

    private static string ReadTextFile(string path)
    {
        File.Exists(path).ShouldBeTrue($"Expected file: {path}");
        return File.ReadAllText(path);
    }

    private static async Task<CommandResult> RunDotNetCommand(
        TimeSpan timeout,
        string workingDirectory,
        params string[] arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        psi.EnvironmentVariables["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        psi.EnvironmentVariables["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";

        foreach (var argument in arguments)
        {
            psi.ArgumentList.Add(argument);
        }

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet process");

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        var exitTask = process.WaitForExitAsync();
        var timeoutTask = Task.Delay(timeout);

        if (await Task.WhenAny(exitTask, timeoutTask) != exitTask)
        {
            TryKill(process);
            throw new TimeoutException(
                $"dotnet {string.Join(' ', arguments)} timed out after {timeout}.");
        }

        await exitTask;

        var output = await outputTask;
        var error = await errorTask;

        return new CommandResult(process.ExitCode, output, error);
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    internal record CommandResult(int ExitCode, string Output, string Error);
}
