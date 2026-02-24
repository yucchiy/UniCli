using System;
using System.IO;
using UnityEditor;
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
            if (!IsRemoteAssemblyIncluded(report))
                return null;

            var path = Path.Combine(data.inputDirectory, "UniCli.Remote.link.xml");
            File.WriteAllText(path, "<linker>\n  <assembly fullname=\"UniCli.Remote\" preserve=\"all\" />\n</linker>\n");
            return path;
        }

        private static bool IsRemoteAssemblyIncluded(BuildReport report)
        {
            // UniCli.Remote asmdef defineConstraints:
            //   "DEVELOPMENT_BUILD || UNITY_EDITOR"
            //   "UNICLI_REMOTE || UNITY_EDITOR"
            // In player builds UNITY_EDITOR is not defined,
            // so both DEVELOPMENT_BUILD and UNICLI_REMOTE must be present.
            if ((report.summary.options & BuildOptions.Development) == 0)
                return false;

            var targetGroup = BuildPipeline.GetBuildTargetGroup(report.summary.platform);
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            return Array.IndexOf(defines.Split(';'), "UNICLI_REMOTE") >= 0;
        }
    }
}
