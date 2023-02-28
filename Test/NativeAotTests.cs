using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Xunit;

using static NodeApi.Test.TestBuilder;

namespace NodeApi.Test;

public class NativeAotTests
{
    private static readonly Dictionary<string, string?> s_builtTestModules = new();

    public static IEnumerable<object[]> TestCases { get; } = ListTestCases(
        (testCaseName) => !testCaseName.Contains("/dynamic_"));

    [Theory]
    [MemberData(nameof(TestCases))]
    public void Test(string id)
    {
        string moduleName = id.Substring(0, id.IndexOf('/'));
        string testCaseName = id.Substring(id.IndexOf('/') + 1);
        string testCasePath = testCaseName.Replace('/', Path.DirectorySeparatorChar);

        string buildLogFilePath = GetBuildLogFilePath(moduleName);
        if (!s_builtTestModules.TryGetValue(moduleName, out string? moduleFilePath))
        {
            moduleFilePath = BuildTestModuleCSharp(moduleName, buildLogFilePath);

            if (moduleFilePath != null)
            {
                BuildTestModuleTypeScript(moduleName);
            }

            s_builtTestModules.Add(moduleName, moduleFilePath);
        }

        if (moduleFilePath == null)
        {
            Assert.Fail("Build failed. Check the log for details: " + buildLogFilePath);
        }

        // TODO: Support compiling TS files to JS.
        string jsFilePath = Path.Join(TestCasesDirectory, moduleName, testCasePath + ".js");

        string runLogFilePath = GetRunLogFilePath("aot", moduleName, testCasePath);
        RunNodeTestCase(jsFilePath, runLogFilePath, new Dictionary<string, string>
        {
            [ModulePathEnvironmentVariableName] = moduleFilePath,
        });
    }

    private static string? BuildTestModuleCSharp(
      string moduleName,
      string logFilePath)
    {
        string projectFilePath = Path.Join(
            TestCasesDirectory, moduleName, moduleName + ".csproj");

        // Auto-generate an empty project file. All project info is inherited from
        // TestCases/Directory.Build.{props,targets}
        File.WriteAllText(projectFilePath, "<Project Sdk=\"Microsoft.NET.Sdk\">\n</Project>\n");

        string runtimeIdentifier = GetCurrentPlatformRuntimeIdentifier();
        var properties = new Dictionary<string, string>
        {
            ["RuntimeIdentifier"] = runtimeIdentifier,
            ["Configuration"] = Configuration,
        };

        string? buildResult = BuildProject(
          projectFilePath,
          targets: new[] { "Restore", "Publish" },
          properties,
          returnProperty: "NativeBinary",
          logFilePath: logFilePath,
          verboseLog: false);

        if (string.IsNullOrEmpty(buildResult))
        {
            return null;
        }

        string moduleFilePath = buildResult.Replace(
            Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        moduleFilePath = Path.ChangeExtension(moduleFilePath, ".node");
        Assert.True(File.Exists(moduleFilePath), "Module file was not built: " + moduleFilePath);
        return moduleFilePath;
    }

    private static void BuildTestModuleTypeScript(string _ /*testCaseName*/)
    {
        // TODO: Compile TypeScript code, if the test uses TS.
        // Reference the generated type definitions from the C#?
    }
}
