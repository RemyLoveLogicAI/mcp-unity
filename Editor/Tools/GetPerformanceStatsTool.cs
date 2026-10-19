using System;
using Unity.Profiling;
using UnityEditor;
using UnityEngine.Profiling;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for reading the most recently completed frame's timing, rendering, and memory
    /// performance metrics, to check whether a recent change caused a regression.
    /// Keeps a small set of ProfilerRecorders running for the lifetime of the Editor session,
    /// since a recorder only has data once at least one frame has elapsed since it started.
    /// </summary>
    public class GetPerformanceStatsTool : McpToolBase
    {
        private static ProfilerRecorder _mainThreadTime;
        private static ProfilerRecorder _renderThreadTime;
        private static ProfilerRecorder _gpuFrameTime;
        private static ProfilerRecorder _drawCalls;
        private static ProfilerRecorder _setPassCalls;
        private static ProfilerRecorder _triangles;
        private static ProfilerRecorder _vertices;
        private static bool _recordersStarted;

        public GetPerformanceStatsTool()
        {
            Name = "get_performance_stats";
            Description = "Reads the most recently completed frame's timing, rendering, and memory " +
                "performance metrics, to check whether a recent change caused a regression";
        }

        public override JObject Execute(JObject parameters)
        {
            EnsureRecordersStarted();

            bool isPlaying = EditorApplication.isPlaying && !EditorApplication.isPaused;
            bool hasFrameData = _mainThreadTime.Valid && _mainThreadTime.Count > 0;

            JObject frameTiming = new JObject
            {
                ["mainThreadMs"] = LastValueAsMs(_mainThreadTime),
                ["renderThreadMs"] = LastValueAsMs(_renderThreadTime),
                ["gpuMs"] = LastValueAsMs(_gpuFrameTime)
            };

            JObject rendering = new JObject
            {
                ["drawCalls"] = LastValue(_drawCalls),
                ["setPassCalls"] = LastValue(_setPassCalls),
                ["triangles"] = LastValue(_triangles),
                ["vertices"] = LastValue(_vertices)
            };

            JObject memory = new JObject
            {
                ["totalAllocatedMB"] = BytesToMB(Profiler.GetTotalAllocatedMemoryLong()),
                ["totalReservedMB"] = BytesToMB(Profiler.GetTotalReservedMemoryLong()),
                ["totalUnusedReservedMB"] = BytesToMB(Profiler.GetTotalUnusedReservedMemoryLong()),
                ["monoHeapMB"] = BytesToMB(Profiler.GetMonoHeapSizeLong()),
                ["monoUsedMB"] = BytesToMB(Profiler.GetMonoUsedSizeLong())
            };

            string message;
            if (!isPlaying)
            {
                message = "Captured performance snapshot (not in Play Mode - frame timing and rendering " +
                    "counters read as zero since nothing is rendering a game frame; memory counters are " +
                    "still accurate)";
            }
            else if (!hasFrameData)
            {
                message = "Started performance recorders - no frame has completed yet since they were " +
                    "created. Call this tool again after a moment in Play Mode to get frame timing data";
            }
            else
            {
                message = "Captured performance snapshot from the most recently completed frame";
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = message,
                ["isPlaying"] = isPlaying,
                ["hasFrameData"] = hasFrameData,
                ["frameTiming"] = frameTiming,
                ["rendering"] = rendering,
                ["memory"] = memory
            };
        }

        /// <summary>
        /// Starts the profiler recorders once per Editor session. They're intentionally never
        /// disposed so later calls can read data from frames that elapsed between calls.
        /// </summary>
        private static void EnsureRecordersStarted()
        {
            if (_recordersStarted)
            {
                return;
            }

            _mainThreadTime = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
            _renderThreadTime = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Render Thread", 1);
            _gpuFrameTime = ProfilerRecorder.StartNew(ProfilerCategory.Render, "GPU Frame Time", 1);
            _drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 1);
            _setPassCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count", 1);
            _triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count", 1);
            _vertices = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count", 1);
            _recordersStarted = true;
        }

        /// <summary>
        /// Reads the last sample from a nanosecond-based timing recorder, as milliseconds.
        /// Returns 0 if the recorder isn't valid or has no data yet.
        /// </summary>
        private static double LastValueAsMs(ProfilerRecorder recorder)
        {
            return recorder.Valid ? recorder.LastValue / 1_000_000.0 : 0;
        }

        /// <summary>
        /// Reads the last sample from a plain-count recorder.
        /// Returns 0 if the recorder isn't valid or has no data yet.
        /// </summary>
        private static long LastValue(ProfilerRecorder recorder)
        {
            return recorder.Valid ? recorder.LastValue : 0;
        }

        private static double BytesToMB(long bytes)
        {
            return Math.Round(bytes / (1024.0 * 1024.0), 2);
        }
    }
}
