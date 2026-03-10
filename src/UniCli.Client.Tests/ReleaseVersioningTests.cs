using System.Text.Json;
using UniCli.Client;

namespace UniCli.Client.Tests;

public class ServerReleaseInfoTests
{
    [Fact]
    public void RecommendedSource_UsesServerScopedTag()
    {
        Assert.Contains("#server/v", ServerReleaseInfo.RecommendedSource);
        Assert.EndsWith(ServerReleaseInfo.RecommendedTag, ServerReleaseInfo.RecommendedSource);
    }
}

public class BuildCheckResultTests
{
    [Fact]
    public void BuildCheckJson_ExposesRecommendedServerMetadata()
    {
        var json = Commands.BuildCheckJson(
            installed: true,
            source: "https://example.invalid/custom.git",
            serverRunning: true,
            clientVersion: "1.1.0",
            serverVersion: "1.0.0",
            recommendedServerVersion: "1.1.0",
            recommendedSource: ServerReleaseInfo.RecommendedSource,
            sourceMatchesRecommended: false,
            serverVersionMatchesRecommended: false);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("1.1.0", root.GetProperty("recommendedServerVersion").GetString());
        Assert.Equal(ServerReleaseInfo.RecommendedSource, root.GetProperty("recommendedSource").GetString());
        Assert.False(root.GetProperty("sourceMatchesRecommended").GetBoolean());
        Assert.False(root.GetProperty("serverVersionMatchesRecommended").GetBoolean());
        Assert.False(root.GetProperty("versionMatch").GetBoolean());
    }

    [Fact]
    public void BuildCheckText_UsesRecommendedServerMessaging()
    {
        var text = Commands.BuildCheckText(
            installed: true,
            source: "https://example.invalid/custom.git",
            serverRunning: true,
            clientVersion: "1.1.0",
            serverVersion: "1.0.0",
            recommendedServerVersion: "1.1.0",
            sourceMatchesRecommended: false,
            serverVersionMatchesRecommended: false);

        Assert.Contains("Package Source: custom or outdated", text);
        Assert.Contains("Recommended Server Version: 1.1.0", text);
        Assert.Contains("Server Version: 1.0.0 (different from the recommended server version)", text);
    }
}
