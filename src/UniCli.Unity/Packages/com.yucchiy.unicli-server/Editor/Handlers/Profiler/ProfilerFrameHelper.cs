using System;
using System.Collections.Generic;
using UnityEditor.Profiling;
using UnityEditorInternal;

namespace UniCli.Server.Editor.Handlers
{
    internal static class ProfilerFrameHelper
    {
        public static (int start, int end) ResolveFrameRange(int startFrame, int endFrame)
        {
            var first = ProfilerDriver.firstFrameIndex;
            var last = ProfilerDriver.lastFrameIndex;

            if (last < first)
                throw new InvalidOperationException("No profiler frames available. Start recording first.");

            var start = startFrame < 0 ? first : Math.Max(startFrame, first);
            var end = endFrame < 0 ? last : Math.Min(endFrame, last);

            if (start > end)
                throw new ArgumentException($"Invalid frame range: start ({start}) > end ({end})");

            return (start, end);
        }

        public static List<ProfilerSampleInfo> CollectAllSamples(HierarchyFrameDataView frameData)
        {
            var rootId = frameData.GetRootItemID();
            var children = new List<int>();
            frameData.GetItemChildren(rootId, children);

            var samples = new List<ProfilerSampleInfo>();
            foreach (var childId in children)
            {
                CollectSamplesRecursive(frameData, childId, samples);
            }

            return samples;
        }

        public static void CollectSamplesRecursive(HierarchyFrameDataView frameData, int itemId, List<ProfilerSampleInfo> results)
        {
            var name = frameData.GetItemName(itemId);
            var totalTime = frameData.GetItemColumnDataAsFloat(itemId, HierarchyFrameDataView.columnTotalTime);
            var selfTime = frameData.GetItemColumnDataAsFloat(itemId, HierarchyFrameDataView.columnSelfTime);
            var calls = (int)frameData.GetItemColumnDataAsFloat(itemId, HierarchyFrameDataView.columnCalls);
            var gcAlloc = (long)frameData.GetItemColumnDataAsFloat(itemId, HierarchyFrameDataView.columnGcMemory);

            results.Add(new ProfilerSampleInfo
            {
                name = name,
                totalTimeMs = totalTime,
                selfTimeMs = selfTime,
                calls = calls,
                gcAllocBytes = gcAlloc
            });

            var children = new List<int>();
            frameData.GetItemChildren(itemId, children);
            foreach (var childId in children)
            {
                CollectSamplesRecursive(frameData, childId, results);
            }
        }

        public static long GetFrameGcAlloc(HierarchyFrameDataView frameData)
        {
            var samples = CollectAllSamples(frameData);
            long total = 0;
            foreach (var s in samples)
            {
                total += s.gcAllocBytes;
            }
            return total;
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes == 0) return "0 B";
            if (bytes < 1024L) return $"{bytes} B";
            if (bytes < 1024L * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024):F1} MB";
        }

        public static string Truncate(string s, int maxLen)
        {
            if (s == null) return "";
            return s.Length <= maxLen ? s : s.Substring(0, maxLen - 3) + "...";
        }
    }
}
