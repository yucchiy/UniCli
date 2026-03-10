namespace UniCli.Client;

internal static class ServerReleaseInfo
{
    public const string RecommendedVersion = "1.1.0";

    private const string DefaultGitUrl =
        "https://github.com/yucchiy/UniCli.git?path=src/UniCli.Unity/Packages/com.yucchiy.unicli-server";

    public static string RecommendedTag => $"server/v{RecommendedVersion}";

    public static string RecommendedSource => $"{DefaultGitUrl}#{RecommendedTag}";
}
