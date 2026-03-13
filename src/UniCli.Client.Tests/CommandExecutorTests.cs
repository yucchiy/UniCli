using System.IO;
using UniCli.Client;
using UniCli.Protocol;

namespace UniCli.Client.Tests;

public class BuildLaunchStartedMessageTests
{
    [Fact]
    public void IncludesAbsoluteProjectRootAndRetryGuidance()
    {
        var result = CommandExecutor.BuildLaunchStartedMessage("src/UniCli.Unity");

        Assert.Contains("Unity Editor was not running, so UniCli started it.", result);
        Assert.Contains(Path.GetFullPath("src/UniCli.Unity"), result);
        Assert.Contains("run the command again", result);
    }

    [Fact]
    public void AssetsPath_IsNormalizedToProjectRoot()
    {
        var result = CommandExecutor.BuildLaunchStartedMessage("src/UniCli.Unity/Assets");

        Assert.Contains(Path.GetFullPath("src/UniCli.Unity"), result);
        Assert.DoesNotContain(Path.GetFullPath("src/UniCli.Unity/Assets"), result);
    }
}

public class FormatTypeDetailHeadingTests
{
    [Fact]
    public void ReturnsTypeName_WhenUnique()
    {
        var detail = new CommandTypeDetail
        {
            typeName = "Duplicate",
            typeId = "Tests:Alpha.Duplicate"
        };

        var result = CommandExecutor.FormatTypeDetailHeading(
            detail,
            new Dictionary<string, int> { ["Duplicate"] = 1 });

        Assert.Equal("Duplicate", result);
    }

    [Fact]
    public void AppendsTypeId_WhenTypeNameCollides()
    {
        var detail = new CommandTypeDetail
        {
            typeName = "Duplicate",
            typeId = "Tests:Alpha.Duplicate"
        };

        var result = CommandExecutor.FormatTypeDetailHeading(
            detail,
            new Dictionary<string, int> { ["Duplicate"] = 2 });

        Assert.Equal("Duplicate (Tests:Alpha.Duplicate)", result);
    }
}
