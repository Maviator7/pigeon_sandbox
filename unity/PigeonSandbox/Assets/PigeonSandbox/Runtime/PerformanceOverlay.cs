using Unity.Profiling;
using UnityEngine;

namespace PigeonSandbox
{
    // Frame time, rendering and GC counters for performance work. F3 toggles it; the benchmark scene keeps it on.
    // Render and GC counters need a Development build; they read "n/a" elsewhere.
    public sealed class PerformanceOverlay : MonoBehaviour
    {
        public bool Visible;
        public string Footer = "";
        ProfilerRecorder drawCalls, batches, setPassCalls, gcAllocated;
        readonly float[] frames = new float[120];
        int frameIndex, frameCount;
        float refresh;
        string label = "";
        GUIStyle style;
        public long DrawCalls => Read(drawCalls);
        public long Batches => Read(batches);
        public long SetPassCalls => Read(setPassCalls);
        public long GcAllocatedBytes => Read(gcAllocated);
        static long Read(ProfilerRecorder recorder) => recorder.Valid ? recorder.LastValue : -1;
        void OnEnable()
        {
            drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            setPassCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            gcAllocated = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        }

        void OnDisable()
        {
            drawCalls.Dispose();
            batches.Dispose();
            setPassCalls.Dispose();
            gcAllocated.Dispose();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F3))
                Visible = !Visible;
            frames[frameIndex] = Time.unscaledDeltaTime;
            frameIndex = (frameIndex + 1) % frames.Length;
            frameCount = Mathf.Min(frameCount + 1, frames.Length);
            refresh -= Time.unscaledDeltaTime;
            if (!Visible || refresh > 0)
                return;
            refresh = .25f;
            float total = 0, worst = 0;
            for (int i = 0; i < frameCount; i++)
            {
                total += frames[i];
                worst = Mathf.Max(worst, frames[i]);
            }

            float average = frameCount > 0 ? total / frameCount : 0;
            label = string.Format("FPS {0:0}  frame {1:0.0}ms (max {2:0.0})\ndraw {3}  batches {4}  setpass {5}\nGC {6}/frame", average > 0 ? 1 / average : 0, average * 1000, worst * 1000, Text(DrawCalls), Text(Batches), Text(SetPassCalls), GcAllocatedBytes < 0 ? "n/a" : (GcAllocatedBytes / 1024f).ToString("0.0") + "KB");
        }

        static string Text(long value) => value < 0 ? "n/a" : value.ToString();
        void OnGUI()
        {
            if (!Visible)
                return;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label)
                {fontSize = 14, wordWrap = true};
                style.normal.textColor = Color.white;
            }

            GUI.depth = -100;
            string content = string.IsNullOrEmpty(Footer) ? label : label + "\n" + Footer;
            float width = 340, height = style.CalcHeight(new GUIContent(content), width - 16) + 12;
            var box = new Rect(8, Screen.height - height - 8, width, height);
            GUI.color = new Color(0, 0, 0, .72f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(box.x + 8, box.y + 6, width - 16, height - 12), content, style);
        }
    }
}
