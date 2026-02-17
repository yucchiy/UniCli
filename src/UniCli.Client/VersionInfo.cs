using System.Reflection;

namespace UniCli.Client;

internal static class VersionInfo
{
    public static string Version => typeof(VersionInfo).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion?.Split('+')[0] ?? "unknown";
}
