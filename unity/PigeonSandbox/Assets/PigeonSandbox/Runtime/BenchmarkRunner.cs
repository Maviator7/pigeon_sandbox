using System;
using System.Collections.Generic;
using UnityEngine;

namespace PigeonSandbox
{
    // Measures the benchmark town after a warm-up and logs one "PIGEON BENCHMARK" line.
    // A policy toggle every few seconds forces the facility rebuild path so its spikes are included.
    // Pass -benchmark-quit on the command line to exit after logging.
    public sealed class BenchmarkRunner : MonoBehaviour
    {
        const float WarmUp = 5, Duration = 20, RebuildInterval = 3;
        TownSimulation town;
        PerformanceOverlay overlay;
        readonly List<float> frameMs = new List<float>();
        // Work per frame from FrameTimingManager: shows headroom even when the display caps the frame rate (iOS at 60Hz).
        readonly List<float> cpuMainMs = new List<float>(), cpuRenderMs = new List<float>(), gpuMs = new List<float>();
        readonly FrameTiming[] timings = new FrameTiming[1];
        long drawCalls, batches, setPassCalls, gcBytes, gcSamples, maxGcBytes;
        int renderSamples, rebuilds;
        float clock, rebuildClock;
        bool done;
        public string Result
        {
            get;
            private set;
        }

        = "";
        public void Begin(TownSimulation town, PerformanceOverlay overlay)
        {
            this.town = town;
            this.overlay = overlay;
            // Uncapped where the platform allows, so the numbers show headroom rather than the 60fps cap.
            QualitySettings.vSyncCount = 0;
            // The player normally pauses while unfocused; keep measuring when launched from a script.
            Application.runInBackground = true;
            Application.targetFrameRate = 1000;
            overlay.Visible = true;
            overlay.Footer = "BENCHMARK: warming up";
        }

        void LateUpdate()
        {
            if (town == null || done)
                return;
            float dt = Time.unscaledDeltaTime;
            clock += dt;
            if (clock < WarmUp)
                return;
            rebuildClock += dt;
            if (rebuildClock >= RebuildInterval)
            {
                rebuildClock = 0;
                town.TogglePolicy(TownPolicy.NestBoxes);
                rebuilds++;
            }

            frameMs.Add(dt * 1000);
            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, timings) > 0)
            {
                if (timings[0].cpuMainThreadFrameTime > 0)
                    cpuMainMs.Add((float)timings[0].cpuMainThreadFrameTime);
                if (timings[0].cpuRenderThreadFrameTime > 0)
                    cpuRenderMs.Add((float)timings[0].cpuRenderThreadFrameTime);
                if (timings[0].gpuFrameTime > 0)
                    gpuMs.Add((float)timings[0].gpuFrameTime);
            }

            if (overlay.DrawCalls >= 0)
            {
                drawCalls += overlay.DrawCalls;
                batches += overlay.Batches;
                setPassCalls += overlay.SetPassCalls;
                renderSamples++;
            }

            if (overlay.GcAllocatedBytes >= 0)
            {
                gcBytes += overlay.GcAllocatedBytes;
                maxGcBytes = Math.Max(maxGcBytes, overlay.GcAllocatedBytes);
                gcSamples++;
            }

            overlay.Footer = "BENCHMARK: measuring " + (clock - WarmUp).ToString("0") + " / " + Duration + "s";
            if (clock - WarmUp < Duration)
                return;
            done = true;
            Finish();
        }

        void Finish()
        {
            frameMs.Sort();
            float total = 0;
            foreach (float ms in frameMs)
                total += ms;
            float average = total / frameMs.Count;
            float p99 = frameMs[Mathf.Min(frameMs.Count - 1, (int)(frameMs.Count * .99f))];
            float max = frameMs[frameMs.Count - 1];
            Result = string.Format("fps={0:0.0} frameMs(avg/p99/max)={1:0.00}/{2:0.00}/{3:0.00} drawCalls={4} batches={5} setPass={6} gcKBPerFrame(avg/max)={7}/{8} rebuilds={9} ui={17} facilities={10} birds={11} visitors={12} device=\"{13}\" gpu=\"{14}\" screen={15}x{16}", 1000 / average, average, p99, max, Average(drawCalls, renderSamples), Average(batches, renderSamples), Average(setPassCalls, renderSamples), gcSamples > 0 ? (gcBytes / 1024f / gcSamples).ToString("0.00") : "n/a", gcSamples > 0 ? (maxGcBytes / 1024f).ToString("0.0") : "n/a", rebuilds, town.Facilities.Count, town.Birds.Count, town.Visitors.Count, SystemInfo.deviceModel, SystemInfo.graphicsDeviceName, Screen.width, Screen.height, Array.IndexOf(Environment.GetCommandLineArgs(), "-benchmark-no-ui") >= 0 ? "off" : "on");
            Result += " cpuMainMs(avg/p99)=" + Summary(cpuMainMs) + " cpuRenderMs(avg/p99)=" + Summary(cpuRenderMs) + " gpuMs(avg/p99)=" + Summary(gpuMs) + " displayHz=" + Screen.currentResolution.refreshRateRatio.value.ToString("0");
            Debug.Log("PIGEON BENCHMARK " + Result);
            overlay.Footer = "BENCHMARK done\n" + Result.Replace(" ", "\n");
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-benchmark-quit") >= 0)
                Application.Quit();
        }

        static string Summary(List<float> samples)
        {
            if (samples.Count == 0)
                return "n/a";
            samples.Sort();
            float total = 0;
            foreach (float ms in samples)
                total += ms;
            return (total / samples.Count).ToString("0.00") + "/" + samples[Mathf.Min(samples.Count - 1, (int)(samples.Count * .99f))].ToString("0.00");
        }

        static string Average(long sum, int samples) => samples > 0 ? (sum / samples).ToString() : "n/a";
    }
}
