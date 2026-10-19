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

        static GetPerformanceStatsTool()
        {
            // Dispose the recorders' native handles before a domain reload wipes the static
            // fields that reference them, otherwise each reload leaks the previous handles.
            AssemblyReloadEvents.beforeAssemblyReload += DisposeRecorders;
        }

        public GetPerformanceStatsTool()
        {
            Name = "get_performance_stats";
            Description = "Reads recent frame timing and rendering metrics, plus current memory usage, " +
                "to check whether a recent change caused a regression";
        }

        public override JObject Execute(JObject parameters)
        {
            EnsureRecordersStarted();

            bool isPlaying = EditorApplication.isPlaying && !EditorApplication.isPaused;
            bool hasFrameData = isPlaying && _mainThreadTime.Valid && _mainThreadTime.Count > 0;

            // Outside Play Mode, the recorders still hold editor-overhead samples (GUI repaints,
            // etc.) that aren't meaningful game performance data - report null rather than pass
            // them through as if they were. Within a valid frame, each metric is also gated on
            // its own recorder's validity (not every counter is supported on every
            // platform/graphics backend), so a caller can tell "not measured" apart from a
            // genuine zero instead of both reading as 0.
            JObject frameTiming = new JObject
            {
                ["mainThreadMs"] = MsOrNull(_mainThreadTime, hasFrameData),
                ["renderThreadMs"] = MsOrNull(_renderThreadTime, hasFrameData),
                ["gpuMs"] = MsOrNull(_gpuFrameTime, hasFrameData)
            };

            JObject rendering = new JObject
            {
                ["drawCalls"] = CountOrNull(_drawCalls, hasFrameData),
                ["setPassCalls"] = CountOrNull(_setPassCalls, hasFrameData),
                ["triangles"] = CountOrNull(_triangles, hasFrameData),
                ["vertices"] = CountOrNull(_vertices, hasFrameData)
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
                    "counters are null since nothing is rendering a game frame; memory counters are " +
                    "still accurate)";
            }
            else if (!hasFrameData)
            {
                message = "Started performance recorders - no frame has completed yet since they were " +
                    "created. Call this tool again after a moment in Play Mode to get frame timing data";
            }
            else
            {
                message = "Captured performance snapshot from the most recently completed frame. " +
                    "A null field means that counter isn't supported on this platform/graphics backend";
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
        /// Starts the profiler recorders once per Editor session. Disposed via
        /// AssemblyReloadEvents.beforeAssemblyReload and restarted lazily on the next call.
        /// </summary>
        private static void EnsureRecordersStarted()
        {
            if (_recordersStarted)
            {
                return;
            }

            _mainThreadTime = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
            // The exact name/category of the render-thread timing counter isn't consistently
            // documented across Unity versions/platforms (it's been reported both as "Render
            // Thread" and, on newer versions, as "CPU Render Thread Frame Time"; some platforms
            // have no separate render thread at all). Try the known candidates and fall back to
            // an invalid recorder (reported as null, not a misleading zero) if none are valid.
            _renderThreadTime = StartFirstValidRecorder(
                (ProfilerCategory.Internal, "CPU Render Thread Frame Time"),
                (ProfilerCategory.Internal, "Render Thread")
            );
            _gpuFrameTime = ProfilerRecorder.StartNew(ProfilerCategory.Render, "GPU Frame Time", 1);
            _drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 1);
            _setPassCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count", 1);
            _triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count", 1);
            _vertices = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count", 1);
            _recordersStarted = true;
        }

        /// <summary>
        /// Tries each (category, statName) candidate in order, keeping the first one that starts
        /// a valid recorder and disposing the rest. Returns the last (invalid) attempt if none
        /// of the candidates are valid on this Unity version/platform.
        /// </summary>
        private static ProfilerRecorder StartFirstValidRecorder(params (ProfilerCategory category, string statName)[] candidates)
        {
            ProfilerRecorder recorder = default;
            for (int i = 0; i < candidates.Length; i++)
            {
                recorder = ProfilerRecorder.StartNew(candidates[i].category, candidates[i].statName, 1);
                if (recorder.Valid)
                {
                    return recorder;
                }

                if (i < candidates.Length - 1)
                {
                    recorder.Dispose();
                }
            }

            return recorder;
        }

        private static void DisposeRecorders()
        {
            if (!_recordersStarted)
            {
                return;
            }

            _mainThreadTime.Dispose();
            _renderThreadTime.Dispose();
            _gpuFrameTime.Dispose();
            _drawCalls.Dispose();
            _setPassCalls.Dispose();
            _triangles.Dispose();
            _vertices.Dispose();
            _recordersStarted = false;
        }

        /// <summary>
        /// Reads the last sample from a nanosecond-based timing recorder, as milliseconds.
        /// Returns null (not 0) if there's no frame to read yet or the recorder isn't valid -
        /// e.g. that counter isn't supported on this platform/graphics backend.
        /// </summary>
        private static double? MsOrNull(ProfilerRecorder recorder, bool hasFrameData)
        {
            return hasFrameData && recorder.Valid ? recorder.LastValue / 1_000_000.0 : (double?)null;
        }

        /// <summary>
        /// Reads the last sample from a plain-count recorder.
        /// Returns null (not 0) if there's no frame to read yet or the recorder isn't valid -
        /// e.g. that counter isn't supported on this platform/graphics backend.
        /// </summary>
        private static long? CountOrNull(ProfilerRecorder recorder, bool hasFrameData)
        {
            return hasFrameData && recorder.Valid ? recorder.LastValue : (long?)null;
        }

        private static double BytesToMB(long bytes)
        {
            return Math.Round(bytes / (1024.0 * 1024.0), 2);
        }
    }
}
