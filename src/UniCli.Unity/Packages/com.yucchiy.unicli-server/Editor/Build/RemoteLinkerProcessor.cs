using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.UnityLinker;

namespace UniCli.Server.Editor.Build
{
    public sealed class RemoteLinkerProcessor : IUnityLinkerProcessor
    {
        public int callbackOrder => 0;

        public string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
        {
            var path = Path.Combine(data.inputDirectory, "UniCli.Remote.link.xml");
            File.WriteAllText(path, "<linker>\n  <assembly fullname=\"UniCli.Remote\" preserve=\"all\" />\n</linker>\n");
            return path;
        }
    }
}
