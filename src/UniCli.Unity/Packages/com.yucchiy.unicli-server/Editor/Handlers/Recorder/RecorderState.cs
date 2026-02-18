#if UNICLI_RECORDER
using UnityEditor.Recorder;

namespace UniCli.Server.Editor.Handlers
{
    internal static class RecorderState
    {
        public static RecorderController Controller { get; set; }
        public static string OutputPath { get; set; }
    }
}
#endif
