using System.Runtime.InteropServices;

namespace UniCli.Client.Tests;

public class CreateStartInfoTests
{
    [SkippableFact]
    public void UsesOpenCommandWithAppBundle_Unix()
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

        var editorPath = "/Applications/Unity/Hub/Editor/6000.0.0f1/Unity.app/Contents/MacOS/Unity";
        var projectRoot = "/tmp/MyProject";

        var startInfo = UnityLauncher.CreateStartInfo(editorPath, projectRoot, isMacOS: true);

        Assert.Equal("open", startInfo.FileName);
        Assert.False(startInfo.UseShellExecute);
        Assert.False(startInfo.RedirectStandardOutput);
        Assert.False(startInfo.RedirectStandardError);
        Assert.Equal(["-a", "/Applications/Unity/Hub/Editor/6000.0.0f1/Unity.app", "--args", "-projectPath", projectRoot], startInfo.ArgumentList);
    }

    [SkippableFact]
    public void UsesDetachedDirectLaunch_Windows()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

        var editorPath = @"C:\Program Files\Unity\Hub\Editor\6000.0.0f1\Editor\Unity.exe";
        var projectRoot = @"C:\work\MyProject";

        var startInfo = UnityLauncher.CreateStartInfo(editorPath, projectRoot, isMacOS: false);

        Assert.Equal(editorPath, startInfo.FileName);
        Assert.True(startInfo.UseShellExecute);
        Assert.False(startInfo.RedirectStandardOutput);
        Assert.False(startInfo.RedirectStandardError);
        Assert.Equal(["-projectPath", projectRoot], startInfo.ArgumentList);
    }
}

public class TryGetMacOSAppBundlePathTests
{
    [Fact]
    public void UnityBinaryInsideAppBundle_ReturnsBundlePath()
    {
        var editorPath = "/Applications/Unity/Hub/Editor/6000.0.0f1/Unity.app/Contents/MacOS/Unity";

        var result = UnityLauncher.TryGetMacOSAppBundlePath(editorPath);

        Assert.Equal("/Applications/Unity/Hub/Editor/6000.0.0f1/Unity.app", result);
    }

    [Fact]
    public void NonBundlePath_ReturnsNull()
    {
        var editorPath = "/opt/unity/Editor/Unity";

        var result = UnityLauncher.TryGetMacOSAppBundlePath(editorPath);

        Assert.Null(result);
    }
}

public class NormalizeProjectRootTests
{
    [Fact]
    public void RelativeProjectRoot_IsConvertedToAbsolutePath()
    {
        var result = UnityLauncher.NormalizeProjectRoot("src/UniCli.Unity");

        Assert.Equal(Path.GetFullPath("src/UniCli.Unity"), result);
    }

    [Fact]
    public void AssetsPath_IsTrimmedToProjectRootAndConvertedToAbsolutePath()
    {
        var result = UnityLauncher.NormalizeProjectRoot("src/UniCli.Unity/Assets");

        Assert.Equal(Path.GetFullPath("src/UniCli.Unity"), result);
    }
}
