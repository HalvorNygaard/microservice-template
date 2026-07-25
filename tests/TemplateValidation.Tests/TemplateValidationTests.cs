using System.Diagnostics;
using System.IO.Compression;

namespace TemplateValidation.Tests;

public class TemplateValidationTests
{
    internal const string TemplateIdentity = "ModernMicroservice.Template";
    internal const string TemplateShortName = "modern-microservice";

    internal static readonly string RepoRoot = FindRepoRoot();
    internal static readonly string DistPath = Path.Combine(RepoRoot, "dist");

    private static readonly string TemplateProjectPath = Path.Combine(RepoRoot, "template");
    private static readonly string NupkgPath = Path.Combine(RepoRoot, ".artifacts", "package", "release");
    private static readonly string TemplateHivePath = Path.Combine(
        RepoRoot,
        ".artifacts",
        "template-hives",
        Guid.NewGuid().ToString("N"));
    private static readonly TimeSpan DefaultCommandTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan GeneratedTestTimeout = TimeSpan.FromMinutes(10);
    private static readonly string[] ExpectedTopLevelEntries =
    [
        ".config",
        ".editorconfig",
        ".gitattributes",
        ".github",
        ".gitignore",
        "AGENTS.md",
        "Directory.Build.props",
        "Directory.Packages.props",
        "LICENSE",
        $"{TestServiceName}.slnx",
        "NuGet.Config",
        "README.md",
        "aspire.config.json",
        "docs",
        "global.json",
        "src",
        "tests"
    ];
    private static readonly string[] ExpectedGeneratedFiles =
    [
        ".config/dotnet-tools.json",
        ".editorconfig",
        ".gitattributes",
        ".github/workflows/ci.yml",
        ".gitignore",
        "AGENTS.md",
        "Directory.Build.props",
        "Directory.Packages.props",
        "LICENSE",
        $"{TestServiceName}.slnx",
        "NuGet.Config",
        "README.md",
        "aspire.config.json",
        "docs/architecture.md",
        "docs/greenfield.md",
        "docs/operations.md",
        "global.json",
        $"src/{TestServiceName}/appsettings.json",
        $"src/{TestServiceName}/Common/Http/ApplicationRequestTimeouts.cs",
        $"src/{TestServiceName}/Common/Http/EndpointMetadataExtensions.cs",
        $"src/{TestServiceName}/Common/Http/GlobalExceptionHandler.cs",
        $"src/{TestServiceName}/Common/Http/GlobalExceptionHandlerObservability.cs",
        $"src/{TestServiceName}/Common/Http/ProblemDetailsExtensions.cs",
        $"src/{TestServiceName}/Common/MicroserviceTelemetry.cs",
        $"src/{TestServiceName}/Configurations/Setup/DevelopmentSetup.cs",
        $"src/{TestServiceName}/Configurations/Setup/MicroserviceSetup.cs",
        $"src/{TestServiceName}/Features/Tasks/Complete/CompleteTask.cs",
        $"src/{TestServiceName}/Features/Tasks/Create/CreateTask.cs",
        $"src/{TestServiceName}/Features/Tasks/Delete/DeleteTask.cs",
        $"src/{TestServiceName}/Features/Tasks/Get/GetTask.cs",
        $"src/{TestServiceName}/Features/Tasks/Internal/Persistence/TaskItemConfiguration.cs",
        $"src/{TestServiceName}/Features/Tasks/TaskItem.cs",
        $"src/{TestServiceName}/Features/Tasks/TaskObservability.cs",
        $"src/{TestServiceName}/Features/Tasks/TaskRepresentation.cs",
        $"src/{TestServiceName}/Features/Tasks/TasksFeature.cs",
        $"src/{TestServiceName}/GlobalSuppressions.cs",
        $"src/{TestServiceName}/Infrastructure/Data/ApplicationDbContext.cs",
        $"src/{TestServiceName}/Infrastructure/Data/DataExtensions.cs",
        $"src/{TestServiceName}/Infrastructure/Data/Migrations/20260725140819_Initial.cs",
        $"src/{TestServiceName}/Infrastructure/Data/Migrations/20260725140819_Initial.Designer.cs",
        $"src/{TestServiceName}/Infrastructure/Data/Migrations/ApplicationDbContextModelSnapshot.cs",
        $"src/{TestServiceName}/{TestServiceName}.csproj",
        $"src/{TestServiceName}/Program.cs",
        $"src/{TestServiceName}/Properties/AssemblyInfo.cs",
        $"src/{TestServiceName}/Properties/launchSettings.json",
        $"src/{TestServiceName}.AppHost/appsettings.json",
        $"src/{TestServiceName}.AppHost/AppHost.cs",
        $"src/{TestServiceName}.AppHost/AssemblyMarker.cs",
        $"src/{TestServiceName}.AppHost/{TestServiceName}.AppHost.csproj",
        $"src/{TestServiceName}.AppHost/Properties/launchSettings.json",
        $"tests/{TestServiceName}.IntegrationTests/Common/ApiAssertions.cs",
        $"tests/{TestServiceName}.IntegrationTests/Common/TestFixture.cs",
        $"tests/{TestServiceName}.IntegrationTests/{TestServiceName}.IntegrationTests.csproj",
        $"tests/{TestServiceName}.IntegrationTests/Tests/TasksApiTests.cs",
        $"tests/{TestServiceName}.UnitTests/{TestServiceName}.UnitTests.csproj",
        $"tests/{TestServiceName}.UnitTests/Platform/HttpFoundationTests.cs",
        $"tests/{TestServiceName}.UnitTests/Tasks/TaskItemTests.cs"
    ];
    private static readonly string[] ForbiddenGeneratedPaths =
    [
        ".aspire",
        "dist",
        "template",
        "tests/TemplateValidation.Tests"
    ];
    private static readonly string[] ForbiddenGeneratedFragments =
    [
        "MicroserviceTemplate",
        "ModernMicroservice",
        "<ServiceName>",
        "microservice-template",
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

    private static string FindRepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MicroserviceTemplate.slnx")) &&
                File.Exists(Path.Combine(directory.FullName, "template", "microservice-template.Template.csproj")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate the template repository above {AppContext.BaseDirectory}.");
    }

    [Test]
    public async Task TemplateGeneratesCleanBuildableService()
    {
        EnsureDistIsEmpty();

        try
        {
            await BuildAndInstallTemplateAsync();

            var outputPath = await GenerateProjectAsync();

            AssertGeneratedProjectTree(outputPath);
            AssertGeneratedFileManifest(outputPath);
            AssertGeneratedMigrationsExist(outputPath);
            AssertGeneratedProjectFilesExist(outputPath);
            AssertGeneratedSolution(outputPath);
            AssertGeneratedProjectReferences(outputPath);
            AssertGeneratedRuntimeDefaults(outputPath);
            AssertGeneratedAppHost(outputPath);
            AssertGeneratedLaunchSettings(outputPath);
            await AssertGeneratedTextDoesNotContainForbiddenFragments(outputPath);
            AssertGeneratedReadme(outputPath);

            await AssertGeneratedToolsRestoreAsync(outputPath);
            await AssertGeneratedSolutionRestoresAsync(outputPath, TestServiceName);
            await AssertGeneratedProjectBuildsAsync(outputPath);
            await AssertGeneratedMigrationsMatchModelAsync(outputPath);
            await AssertGeneratedProjectPublishesAsync(outputPath);
            await AssertGeneratedTestsPassAsync(outputPath);
            await AssertCommonServiceNamesBuildAsync();
        }
        finally
        {
            CleanupDist();
            CleanupTemplateHive();
        }
    }

    internal static async Task<CommandResult> RunDotNetCommand(string workingDirectory, params string[] arguments)
    {
        return await RunDotNetCommand(DefaultCommandTimeout, workingDirectory, arguments);
    }

    private static Task<string> GenerateProjectAsync() =>
        GenerateProjectAsync(TestServiceName, TestOutputDirectoryName, skipRestore: true);

    private static async Task<string> GenerateProjectAsync(
        string serviceName,
        string outputDirectoryName,
        bool skipRestore)
    {
        var outputPath = Path.Combine(DistPath, outputDirectoryName);
        var arguments = new List<string>
        {
            "new",
            TemplateShortName,
            "--name",
            serviceName,
            "--output",
            outputPath
        };
        if (skipRestore)
        {
            arguments.Add("--no-restore");
        }

        var createResult = await RunDotNetCommand(
            RepoRoot,
            arguments.ToArray());

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

    private static void AssertGeneratedFileManifest(string outputPath)
    {
        var actualFiles = Directory.GetFiles(outputPath, "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(outputPath, file).Replace('\\', '/'))
            .Where(static path => !path.Split('/').Any(static part => part is "bin" or "obj"))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();
        var expectedFiles = ExpectedGeneratedFiles
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();

        actualFiles.ShouldBe(
            expectedFiles,
            "Generated project file manifest changed. Intended additions require an explicit manifest update.");
    }

    private static void AssertGeneratedMigrationsExist(string outputPath)
    {
        var migrationsPath = Path.Combine(outputPath, "src", TestServiceName, "Infrastructure", "Data", "Migrations");
        var migrationFiles = Directory.GetFiles(migrationsPath, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .ToArray();

        migrationFiles.ShouldContain("ApplicationDbContextModelSnapshot.cs");
        migrationFiles.Count(static file => file is not null && file.EndsWith("_Initial.cs", StringComparison.Ordinal)).ShouldBe(1);
        migrationFiles.Count(static file => file is not null && file.EndsWith("_Initial.Designer.cs", StringComparison.Ordinal)).ShouldBe(1);
    }

    private static void AssertGeneratedProjectFilesExist(string outputPath)
    {
        var filesToCheck = new[]
        {
            Path.Combine(outputPath, TestServiceName + ".slnx"),
            Path.Combine(outputPath, ".config", "dotnet-tools.json"),
            Path.Combine(outputPath, ".github", "workflows", "ci.yml"),
            Path.Combine(outputPath, "AGENTS.md"),
            Path.Combine(outputPath, "aspire.config.json"),
            Path.Combine(outputPath, "docs", "architecture.md"),
            Path.Combine(outputPath, "docs", "greenfield.md"),
            Path.Combine(outputPath, "docs", "operations.md"),
            Path.Combine(outputPath, "src", TestServiceName, $"{TestServiceName}.csproj"),
            Path.Combine(outputPath, "src", TestServiceName, "Program.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Configurations", "Setup", "DevelopmentSetup.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Configurations", "Setup", "MicroserviceSetup.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Common", "Http", "ApplicationRequestTimeouts.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Common", "Http", "EndpointMetadataExtensions.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Common", "MicroserviceTelemetry.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Common", "Http", "GlobalExceptionHandler.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Common", "Http", "GlobalExceptionHandlerObservability.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Common", "Http", "ProblemDetailsExtensions.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Features", "Tasks", "TasksFeature.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Features", "Tasks", "TaskObservability.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Features", "Tasks", "TaskItem.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Features", "Tasks", "TaskRepresentation.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Features", "Tasks", "Internal", "Persistence", "TaskItemConfiguration.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Features", "Tasks", "Create", "CreateTask.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Features", "Tasks", "Get", "GetTask.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Features", "Tasks", "Complete", "CompleteTask.cs"),
            Path.Combine(outputPath, "src", TestServiceName, "Features", "Tasks", "Delete", "DeleteTask.cs"),
            Path.Combine(outputPath, "src", $"{TestServiceName}.AppHost", $"{TestServiceName}.AppHost.csproj"),
            Path.Combine(outputPath, "src", $"{TestServiceName}.AppHost", "AppHost.cs"),
            Path.Combine(outputPath, "src", $"{TestServiceName}.AppHost", "AssemblyMarker.cs"),
            Path.Combine(outputPath, "src", $"{TestServiceName}.AppHost", "Properties", "launchSettings.json"),
            Path.Combine(outputPath, "tests", $"{TestServiceName}.UnitTests", $"{TestServiceName}.UnitTests.csproj"),
            Path.Combine(outputPath, "tests", $"{TestServiceName}.UnitTests", "Platform", "HttpFoundationTests.cs"),
            Path.Combine(outputPath, "tests", $"{TestServiceName}.UnitTests", "Tasks", "TaskItemTests.cs"),
            Path.Combine(outputPath, "tests", $"{TestServiceName}.IntegrationTests", $"{TestServiceName}.IntegrationTests.csproj"),
            Path.Combine(outputPath, "tests", $"{TestServiceName}.IntegrationTests", "Common", "ApiAssertions.cs"),
            Path.Combine(outputPath, "tests", $"{TestServiceName}.IntegrationTests", "Common", "TestFixture.cs"),
            Path.Combine(outputPath, "tests", $"{TestServiceName}.IntegrationTests", "Tests", "TasksApiTests.cs")
        };

        foreach (var file in filesToCheck)
        {
            File.Exists(file).ShouldBeTrue($"Expected generated file: {file}");
        }

        Directory.GetFiles(outputPath, "packages.lock.json", SearchOption.AllDirectories)
            .ShouldBeEmpty("Generated projects must not contain NuGet lock files.");
    }

    private static void AssertGeneratedSolution(string outputPath)
    {
        var slnFile = Path.Combine(outputPath, $"{TestServiceName}.slnx");
        var slnContent = ReadTextFile(slnFile);

        slnContent.ShouldContain($"<Project Path=\"src/{TestServiceName}/{TestServiceName}.csproj\" />");
        slnContent.ShouldContain($"<Project Path=\"src/{TestServiceName}.AppHost/{TestServiceName}.AppHost.csproj\" />");
        slnContent.ShouldContain($"<Project Path=\"tests/{TestServiceName}.IntegrationTests/{TestServiceName}.IntegrationTests.csproj\" />");
        slnContent.ShouldContain($"<Project Path=\"tests/{TestServiceName}.UnitTests/{TestServiceName}.UnitTests.csproj\" />");
        slnContent.ShouldNotContain("TemplateValidation.Tests");
        slnContent.ShouldNotContain("MicroserviceTemplate");
        slnContent.ShouldNotContain("microservice-template");
    }

    private static void AssertGeneratedProjectReferences(string outputPath)
    {
        var serviceProject = ReadTextFile(Path.Combine(outputPath, "src", TestServiceName, $"{TestServiceName}.csproj"));
        var appHostProject = ReadTextFile(Path.Combine(outputPath, "src", $"{TestServiceName}.AppHost", $"{TestServiceName}.AppHost.csproj"));
        var integrationProject = ReadTextFile(Path.Combine(outputPath, "tests", $"{TestServiceName}.IntegrationTests", $"{TestServiceName}.IntegrationTests.csproj"));
        var unitProject = ReadTextFile(Path.Combine(outputPath, "tests", $"{TestServiceName}.UnitTests", $"{TestServiceName}.UnitTests.csproj"));
        var buildProperties = ReadTextFile(Path.Combine(outputPath, "Directory.Build.props"));

        serviceProject.ShouldContain("<RootNamespace>MyAwesomeService</RootNamespace>");
        serviceProject.ShouldNotContain("<TargetFramework>");
        serviceProject.ShouldNotContain("<ImplicitUsings>");
        serviceProject.ShouldNotContain("<Nullable>");
        serviceProject.ShouldContain("<ContainerRepository>my-awesome-service</ContainerRepository>");
        serviceProject.ShouldContain("<ContainerImageFormat>OCI</ContainerImageFormat>");
        serviceProject.ShouldContain("<ContainerPort Include=\"8080\" Type=\"tcp\" />");
        serviceProject.ShouldContain("<ContainerEnvironmentVariable Include=\"ASPNETCORE_HTTP_PORTS\" Value=\"8080\" />");
        serviceProject.ShouldNotContain("<ContainerFamily>");
        serviceProject.ShouldNotContain("<ContainerUser>");
        serviceProject.ShouldNotContain("<RuntimeIdentifiers>");

        appHostProject.ShouldContain($"<ProjectReference Include=\"..\\{TestServiceName}\\{TestServiceName}.csproj\" />");
        appHostProject.ShouldContain("<RootNamespace>MyAwesomeService.AppHost</RootNamespace>");
        integrationProject.ShouldContain($"<ProjectReference Include=\"..\\..\\src\\{TestServiceName}\\{TestServiceName}.csproj\" />");
        integrationProject.ShouldContain($"<ProjectReference Include=\"..\\..\\src\\{TestServiceName}.AppHost\\{TestServiceName}.AppHost.csproj\" />");
        integrationProject.ShouldContain("<RootNamespace>MyAwesomeService.IntegrationTests</RootNamespace>");
        unitProject.ShouldContain($"<ProjectReference Include=\"..\\..\\src\\{TestServiceName}\\{TestServiceName}.csproj\" />");
        unitProject.ShouldContain("<RootNamespace>MyAwesomeService.UnitTests</RootNamespace>");

        buildProperties.ShouldContain("<TargetFramework>net10.0</TargetFramework>");
        buildProperties.ShouldContain("<ImplicitUsings>enable</ImplicitUsings>");
        buildProperties.ShouldContain("<Nullable>enable</Nullable>");
    }

    private static void AssertGeneratedRuntimeDefaults(string outputPath)
    {
        var serviceRoot = Path.Combine(outputPath, "src", TestServiceName);
        var developmentSetup = ReadTextFile(
            Path.Combine(serviceRoot, "Configurations", "Setup", "DevelopmentSetup.cs"));
        var microserviceSetup = ReadTextFile(
            Path.Combine(serviceRoot, "Configurations", "Setup", "MicroserviceSetup.cs"));
        var dataSetup = ReadTextFile(
            Path.Combine(serviceRoot, "Infrastructure", "Data", "DataExtensions.cs"));
        var integrationTests = ReadTextFile(Path.Combine(
            outputPath,
            "tests",
            $"{TestServiceName}.IntegrationTests",
            "Tests",
            "TasksApiTests.cs"));

        developmentSetup.ShouldContain("app.MapOpenApi();");
        developmentSetup.ShouldContain("app.MapScalarApiReference();");
        developmentSetup.ShouldNotContain("MapGet(\"/\"");

        microserviceSetup.ShouldContain(".WithMetrics(");
        microserviceSetup.ShouldContain(".WithTracing(");
        microserviceSetup.ShouldNotContain(".ConfigureResource(");
        microserviceSetup.ShouldNotContain("ServiceInstanceId");

        dataSetup.ShouldContain("settings => settings.DisableRetry = true");
        integrationTests.ShouldContain("/openapi/v1.json");
        integrationTests.ShouldContain("/scalar/v1");
    }

    private static void AssertGeneratedAppHost(string outputPath)
    {
        var appHost = ReadTextFile(Path.Combine(outputPath, "src", $"{TestServiceName}.AppHost", "AppHost.cs"));

        appHost.ShouldContain("builder.AddProject(");
        appHost.ShouldContain($"\"{TestServiceKebabName}\",");
        appHost.ShouldContain($"\"../{TestServiceName}/{TestServiceName}.csproj\",");
        appHost.ShouldNotContain("Projects.");
        appHost.ShouldContain(".WithReference(postgresdb).WaitFor(postgresdb)");
        appHost.ShouldContain(".WithHttpHealthCheck(\"/health\", endpointName: \"http\")");
        appHost.ShouldContain(".WithUrlForEndpoint(\"http\"");
        appHost.ShouldContain("url.Url = \"/scalar/v1\"");
        appHost.ShouldNotContain("AddRedis");

        var fixture = ReadTextFile(Path.Combine(
            outputPath,
            "tests",
            $"{TestServiceName}.IntegrationTests",
            "Common",
            "TestFixture.cs"));
        fixture.ShouldContain($"CreateAsync<{TestServiceName}.AppHost.AssemblyMarker>");
    }

    private static void AssertGeneratedLaunchSettings(string outputPath)
    {
        var launchSettings = ReadTextFile(Path.Combine(outputPath, "src", $"{TestServiceName}.AppHost", "Properties", "launchSettings.json"));

        launchSettings.ShouldContain("http://localhost:");
        launchSettings.ShouldNotContain(".dev.localhost");
        launchSettings.ShouldNotContain("\"https\"");
        launchSettings.ShouldNotContain("ASPIRE_DASHBOARD_MCP_ENDPOINT_URL");
    }

    private static void AssertGeneratedReadme(string outputPath)
    {
        var readme = ReadTextFile(Path.Combine(outputPath, "README.md"));

        readme.ShouldContain($"# {TestServiceName}");
        readme.ShouldContain("aspire start --non-interactive");
        readme.ShouldContain("docs/greenfield.md");
        readme.ShouldNotContain("<ServiceName>");

        var aspireConfig = ReadTextFile(Path.Combine(outputPath, "aspire.config.json"));
        aspireConfig.ShouldContain($"src/{TestServiceName}.AppHost/{TestServiceName}.AppHost.csproj");

        var agentGuide = ReadTextFile(Path.Combine(outputPath, "AGENTS.md"));
        agentGuide.ShouldContain("owned .NET microservice");
        agentGuide.ShouldNotContain("reusable .NET microservice template");
        agentGuide.ShouldContain($"raw service identity is `{TestServiceName}`");
        agentGuide.ShouldContain($"C# root namespace is `{TestServiceName}`");
        agentGuide.ShouldContain($"dotnet build {TestServiceName}.slnx -c Release");
        agentGuide.ShouldContain(
            $"tests/{TestServiceName}.UnitTests/{TestServiceName}.UnitTests.csproj");
        agentGuide.ShouldContain(
            $"tests/{TestServiceName}.IntegrationTests/{TestServiceName}.IntegrationTests.csproj");
        agentGuide.ShouldContain("docs/greenfield.md");

        var workflow = ReadTextFile(Path.Combine(outputPath, ".github", "workflows", "ci.yml"));
        workflow.ShouldContain("actions/checkout@");
        workflow.ShouldContain("actions/setup-dotnet@");
        workflow.ShouldContain("dotnet tool restore");
        workflow.ShouldContain($"dotnet build {TestServiceName}.slnx -c Release --no-restore");
        workflow.ShouldContain(
            $"dotnet publish src/{TestServiceName}/{TestServiceName}.csproj -c Release --no-build --no-restore");
        workflow.ShouldContain(
            $"dotnet ef migrations has-pending-model-changes --project src/{TestServiceName}/{TestServiceName}.csproj");
        workflow.ShouldContain($"dotnet test --project tests/{TestServiceName}.UnitTests/{TestServiceName}.UnitTests.csproj");
        workflow.ShouldContain($"dotnet test --project tests/{TestServiceName}.IntegrationTests/{TestServiceName}.IntegrationTests.csproj");
        workflow.ShouldContain("jobs:\n  verify:");
        workflow.ShouldContain("--no-build --no-restore");
        workflow.ShouldContain("persist-credentials: false");
        workflow.ShouldNotContain("\n    uses:");
        workflow.ShouldNotContain("secrets.");
        workflow.ShouldNotContain("dotnet run --project");
        workflow.ShouldNotContain("PublishContainer");
        workflow.ShouldNotContain("-p:ContainerBaseImage");
        workflow.ShouldNotContain("-p:ContainerUser");
        workflow.ShouldNotContain("MicroserviceTemplate");
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
        var buildResult = await RunDotNetCommand(
            GeneratedTestTimeout,
            outputPath,
            "build",
            $"{TestServiceName}.slnx",
            "-c",
            "Release",
            "--no-restore");

        buildResult.ExitCode.ShouldBe(0, $"Build failed with output: {buildResult.Output}\nErrors: {buildResult.Error}");
    }

    private static async Task AssertGeneratedSolutionRestoresAsync(string outputPath, string serviceName)
    {
        var solutionName = $"{serviceName}.slnx";
        var restoreResult = await RunDotNetCommand(
            GeneratedTestTimeout,
            outputPath,
            "restore",
            solutionName);

        restoreResult.ExitCode.ShouldBe(
            0,
            $"Restore of exact generated solution '{solutionName}' failed: " +
            $"{restoreResult.Output}\n{restoreResult.Error}");
    }

    private static async Task AssertGeneratedToolsRestoreAsync(string outputPath)
    {
        var result = await RunDotNetCommand(outputPath, "tool", "restore");
        result.ExitCode.ShouldBe(0, $"Tool restore failed: {result.Output}\n{result.Error}");
    }

    private static async Task AssertGeneratedMigrationsMatchModelAsync(string outputPath)
    {
        var serviceProject = Path.Combine("src", TestServiceName, $"{TestServiceName}.csproj");
        var result = await RunDotNetCommand(
            GeneratedTestTimeout,
            outputPath,
            "ef",
            "migrations",
            "has-pending-model-changes",
            "--project",
            serviceProject,
            "--startup-project",
            serviceProject,
            "--no-build",
            "--configuration",
            "Release");

        result.ExitCode.ShouldBe(0, $"Migration drift detected: {result.Output}\n{result.Error}");
    }

    private static async Task AssertGeneratedProjectPublishesAsync(string outputPath)
    {
        var serviceProject = Path.Combine("src", TestServiceName, $"{TestServiceName}.csproj");
        var result = await RunDotNetCommand(
            GeneratedTestTimeout,
            outputPath,
            "publish",
            serviceProject,
            "-c",
            "Release",
            "--no-build",
            "--no-restore");

        result.ExitCode.ShouldBe(0, $"Publish failed: {result.Output}\n{result.Error}");
    }

    private static async Task AssertGeneratedTestsPassAsync(string outputPath)
    {
        string[] testProjects =
        [
            Path.Combine("tests", $"{TestServiceName}.UnitTests", $"{TestServiceName}.UnitTests.csproj"),
            Path.Combine("tests", $"{TestServiceName}.IntegrationTests", $"{TestServiceName}.IntegrationTests.csproj")
        ];

        foreach (string testProject in testProjects)
        {
            var testResult = await RunDotNetCommand(
                GeneratedTestTimeout,
                outputPath,
                "test",
                "--project",
                testProject,
                "-c",
                "Release",
                "--no-build",
                "--no-restore");

            testResult.ExitCode.ShouldBe(
                0,
                $"Generated test project '{testProject}' failed: {testResult.Output}\n{testResult.Error}");
        }
    }

    private static async Task AssertCommonServiceNamesBuildAsync()
    {
        (string ServiceName, string OutputDirectory, string ExpectedNamespace, string ExpectedKebabName)[] variants =
        [
            ("sample-api", "name-with-hyphen", "SampleApi", "sample-api"),
            ("ms-edi", "name-with-ms-prefix", "MsEdi", "ms-edi"),
            ("Example.Service", "name-with-dots", "Example.Service", "example-service")
        ];

        foreach ((
            string serviceName,
            string outputDirectory,
            string expectedNamespace,
            string expectedKebabName) in variants)
        {
            string outputPath = await GenerateProjectAsync(serviceName, outputDirectory, skipRestore: true);
            AssertGeneratedVariantPaths(outputPath, serviceName);
            AssertGeneratedVariantSubstitutions(
                outputPath,
                serviceName,
                expectedNamespace,
                expectedKebabName);
            Directory.GetFiles(outputPath, "packages.lock.json", SearchOption.AllDirectories)
                .ShouldBeEmpty($"Generated service '{serviceName}' must not contain NuGet lock files.");

            await AssertGeneratedSolutionRestoresAsync(outputPath, serviceName);
            var buildResult = await RunDotNetCommand(
                GeneratedTestTimeout,
                outputPath,
                "build",
                $"{serviceName}.slnx",
                "-c",
                "Release",
                "--no-restore");

            buildResult.ExitCode.ShouldBe(
                0,
                $"Generated service name '{serviceName}' did not build: {buildResult.Output}\n{buildResult.Error}");

            await AssertGeneratedTextDoesNotContainForbiddenFragments(outputPath);
        }
    }

    private static void AssertGeneratedVariantPaths(string outputPath, string serviceName)
    {
        string[] expectedPaths =
        [
            $"{serviceName}.slnx",
            $"src/{serviceName}/{serviceName}.csproj",
            $"src/{serviceName}.AppHost/{serviceName}.AppHost.csproj",
            $"tests/{serviceName}.UnitTests/{serviceName}.UnitTests.csproj",
            $"tests/{serviceName}.IntegrationTests/{serviceName}.IntegrationTests.csproj"
        ];

        foreach (string expectedPath in expectedPaths)
        {
            File.Exists(Path.Combine(outputPath, expectedPath.Replace('/', Path.DirectorySeparatorChar)))
                .ShouldBeTrue($"Expected generated path '{expectedPath}'.");
        }

        if (!serviceName.Contains('-', StringComparison.Ordinal))
        {
            return;
        }

        string underscoredName = serviceName.Replace('-', '_');
        var underscoredPaths = Directory.GetFileSystemEntries(outputPath, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(outputPath, path))
            .Where(path => path.Contains(underscoredName, StringComparison.Ordinal))
            .ToArray();
        underscoredPaths.ShouldBeEmpty(
            $"Hyphenated service identity '{serviceName}' must not become '{underscoredName}' in generated paths.");
    }

    private static void AssertGeneratedVariantSubstitutions(
        string outputPath,
        string serviceName,
        string expectedNamespace,
        string expectedKebabName)
    {
        var solution = ReadTextFile(Path.Combine(outputPath, $"{serviceName}.slnx"));
        solution.ShouldContain($"<Project Path=\"src/{serviceName}/{serviceName}.csproj\" />");
        solution.ShouldContain(
            $"<Project Path=\"src/{serviceName}.AppHost/{serviceName}.AppHost.csproj\" />");
        solution.ShouldContain(
            $"<Project Path=\"tests/{serviceName}.UnitTests/{serviceName}.UnitTests.csproj\" />");
        solution.ShouldContain(
            $"<Project Path=\"tests/{serviceName}.IntegrationTests/{serviceName}.IntegrationTests.csproj\" />");

        var serviceProject = ReadTextFile(
            Path.Combine(outputPath, "src", serviceName, $"{serviceName}.csproj"));
        var appHostProject = ReadTextFile(
            Path.Combine(outputPath, "src", $"{serviceName}.AppHost", $"{serviceName}.AppHost.csproj"));
        var unitProject = ReadTextFile(
            Path.Combine(outputPath, "tests", $"{serviceName}.UnitTests", $"{serviceName}.UnitTests.csproj"));
        var integrationProject = ReadTextFile(
            Path.Combine(
                outputPath,
                "tests",
                $"{serviceName}.IntegrationTests",
                $"{serviceName}.IntegrationTests.csproj"));
        var assemblyInfo = ReadTextFile(
            Path.Combine(outputPath, "src", serviceName, "Properties", "AssemblyInfo.cs"));
        var telemetrySource = ReadTextFile(
            Path.Combine(outputPath, "src", serviceName, "Common", "MicroserviceTelemetry.cs"));
        var appHost = ReadTextFile(
            Path.Combine(outputPath, "src", $"{serviceName}.AppHost", "AppHost.cs"));

        serviceProject.ShouldContain($"<RootNamespace>{expectedNamespace}</RootNamespace>");
        serviceProject.ShouldContain($"<ContainerRepository>{expectedKebabName}</ContainerRepository>");
        appHostProject.ShouldContain($"<RootNamespace>{expectedNamespace}.AppHost</RootNamespace>");
        appHostProject.ShouldContain(
            $"<ProjectReference Include=\"..\\{serviceName}\\{serviceName}.csproj\" />");
        unitProject.ShouldContain($"<RootNamespace>{expectedNamespace}.UnitTests</RootNamespace>");
        unitProject.ShouldContain(
            $"<ProjectReference Include=\"..\\..\\src\\{serviceName}\\{serviceName}.csproj\" />");
        integrationProject.ShouldContain(
            $"<RootNamespace>{expectedNamespace}.IntegrationTests</RootNamespace>");
        integrationProject.ShouldContain(
            $"<ProjectReference Include=\"..\\..\\src\\{serviceName}\\{serviceName}.csproj\" />");
        integrationProject.ShouldContain(
            $"<ProjectReference Include=\"..\\..\\src\\{serviceName}.AppHost\\{serviceName}.AppHost.csproj\" />");
        assemblyInfo.ShouldContain($"InternalsVisibleTo(\"{serviceName}.UnitTests\")");
        telemetrySource.ShouldContain($"namespace {expectedNamespace}.Common;");
        telemetrySource.ShouldContain($"MeterName = \"{expectedNamespace}\"");
        appHost.ShouldContain($"\"{expectedKebabName}\",");
        appHost.ShouldContain($"\"../{serviceName}/{serviceName}.csproj\",");

        var workflow = ReadTextFile(Path.Combine(outputPath, ".github", "workflows", "ci.yml"));
        workflow.ShouldContain($"dotnet restore {serviceName}.slnx");
        workflow.ShouldContain($"dotnet format {serviceName}.slnx --verify-no-changes --no-restore");
        workflow.ShouldContain($"dotnet build {serviceName}.slnx -c Release --no-restore");
        workflow.ShouldContain(
            $"dotnet publish src/{serviceName}/{serviceName}.csproj -c Release --no-build --no-restore");
        workflow.ShouldContain(
            $"dotnet ef migrations has-pending-model-changes --project src/{serviceName}/{serviceName}.csproj");
        workflow.ShouldContain(
            $"dotnet test --project tests/{serviceName}.UnitTests/{serviceName}.UnitTests.csproj");
        workflow.ShouldContain(
            $"dotnet test --project tests/{serviceName}.IntegrationTests/{serviceName}.IntegrationTests.csproj");

        var readme = ReadTextFile(Path.Combine(outputPath, "README.md"));
        var architecture = ReadTextFile(Path.Combine(outputPath, "docs", "architecture.md"));
        var operations = ReadTextFile(Path.Combine(outputPath, "docs", "operations.md"));
        var agentGuide = ReadTextFile(Path.Combine(outputPath, "AGENTS.md"));
        var aspireConfig = ReadTextFile(Path.Combine(outputPath, "aspire.config.json"));
        readme.ShouldContain($"# {serviceName}");
        readme.ShouldContain($"src/{serviceName}/{serviceName}.csproj");
        readme.ShouldContain($"tests/{serviceName}.UnitTests/{serviceName}.UnitTests.csproj");
        architecture.ShouldContain($"{serviceName}/");
        architecture.ShouldContain($"{serviceName}.AppHost/");
        operations.ShouldContain($"src/{serviceName}/{serviceName}.csproj");
        agentGuide.ShouldContain($"raw service identity is `{serviceName}`");
        agentGuide.ShouldContain($"C# root namespace is `{expectedNamespace}`");
        agentGuide.ShouldContain($"dotnet build {serviceName}.slnx -c Release");
        aspireConfig.ShouldContain($"src/{serviceName}.AppHost/{serviceName}.AppHost.csproj");

        if (serviceName.Contains('-', StringComparison.Ordinal))
        {
            string underscoredName = serviceName.Replace('-', '_');
            var underscoredContent = EnumerateTextFiles(outputPath)
                .Where(file => File.ReadAllText(file).Contains(underscoredName, StringComparison.Ordinal))
                .Select(file => Path.GetRelativePath(outputPath, file))
                .ToArray();
            underscoredContent.ShouldBeEmpty(
                $"Hyphenated service identity '{serviceName}' must not become '{underscoredName}' in generated content.");
        }

        ReadTextFile(Path.Combine(outputPath, ".editorconfig")).ShouldNotContain("CA1707");
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
        AssertPackagedContentManifest(nupkgFiles[0]);

        var installResult = await RunDotNetCommand(RepoRoot, "new", "install", nupkgFiles[0]);
        installResult.ExitCode.ShouldBe(0, $"Install failed: {installResult.Output}\n{installResult.Error}");
        installResult.Output.ShouldContain(TemplateIdentity);
    }

    private static void AssertPackagedContentManifest(string packagePath)
    {
        using var package = ZipFile.OpenRead(packagePath);
        var actualContentEntries = package.Entries
            .Where(static entry => entry.FullName.StartsWith("content/", StringComparison.Ordinal))
            .Where(static entry => !entry.FullName.EndsWith('/'))
            .Select(static entry => entry.FullName)
            .OrderBy(static entry => entry, StringComparer.Ordinal)
            .ToArray();
        var expectedContentEntries = ExpectedGeneratedFiles
            .Select(path => $"content/{path.Replace(TestServiceName, "MicroserviceTemplate", StringComparison.Ordinal)}")
            .Append("content/.template.config/.templateignore")
            .Append("content/.template.config/dotnetcli.host.json")
            .Append("content/.template.config/template.json")
            .OrderBy(static entry => entry, StringComparer.Ordinal)
            .ToArray();

        actualContentEntries.ShouldBe(
            expectedContentEntries,
            "Packaged template content changed. Intended additions require an explicit manifest update.");
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

    private static void CleanupTemplateHive()
    {
        if (!Directory.Exists(TemplateHivePath))
        {
            return;
        }

        try
        {
            Directory.Delete(TemplateHivePath, recursive: true);
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
        psi.EnvironmentVariables["ConnectionStrings__postgresdb"] =
            "Host=localhost;Port=5432;Database=template_validation;Username=postgres;Password=development-only";

        foreach (var argument in arguments)
        {
            psi.ArgumentList.Add(argument);
        }

        if (arguments.Length > 0 && arguments[0] == "new")
        {
            psi.ArgumentList.Add("--debug:custom-hive");
            psi.ArgumentList.Add(TemplateHivePath);
        }

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet process");

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        using var timeoutSource = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
        {
            TryKill(process);
            await WaitForExitAfterKillAsync(process);
            throw new TimeoutException(
                $"dotnet {string.Join(' ', arguments)} timed out after {timeout}.");
        }

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

    private static async Task WaitForExitAfterKillAsync(Process process)
    {
        try
        {
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or TimeoutException)
        {
            // The original command timeout remains the useful failure.
        }
    }

    internal sealed record CommandResult(int ExitCode, string Output, string Error);
}
