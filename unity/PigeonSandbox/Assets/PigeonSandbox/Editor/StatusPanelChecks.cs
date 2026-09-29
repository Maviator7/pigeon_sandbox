using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PigeonSandbox.Editor
{
    public static class StatusPanelChecks
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static object Call(SandboxApp app, string name, params object[] args)
        {
            var method = typeof(SandboxApp).GetMethod(name, Private);
            if (method == null)
                throw new Exception("Missing status panel API: " + name);
            return method.Invoke(app, args);
        }

        static void Set(SandboxApp app, string name, object value)
        {
            var field = typeof(SandboxApp).GetField(name, Private);
            if (field == null)
                throw new Exception("Missing status panel field: " + name);
            field.SetValue(app, value);
        }

        static T Read<T>(object value, string name)
        {
            var field = value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
                throw new Exception("Missing status panel value: " + name);
            return (T)field.GetValue(value);
        }

        static void Check(bool condition, string message)
        {
            if (!condition)
                throw new Exception(message);
            Debug.Log("STATUS PANEL PASS: " + message);
        }

        static bool Contains(object contents, string value) => Read<string>(contents, "firstText") == value || Read<string>(contents, "secondText") == value;
        [MenuItem("Pigeon Sandbox/Verify status panel")]
        public static void Verify()
        {
            var root = new GameObject("Status panel verification");
            try
            {
                var app = root.AddComponent<SandboxApp>();
                var town = new TownSimulation(9);
                Set(app, "town", town);
                Call(app, "SelectBuilding", FacilityKind.ClockTower);
                town.Notice = "まちの会計：収入 47 / 維持費 88.0";
                object contents = Call(app, "ComposeFooterMessages");
                Check(!Contains(contents, "観光と目立ちたがりの鳩のための名所。静かな住宅からは距離を。") && Read<string>(contents, "firstText") == null, "selected facility tip and accounting stay out of news");
                town.Notice = "新しい鳩が引っ越してきました！";
                contents = Call(app, "ComposeFooterMessages");
                Check(Contains(contents, town.Notice) && Read<string>(contents, "firstLabel") == "できごと", "pigeon arrival remains visible");
                town.Notice = "空き地と建設費を確認しよう。";
                contents = Call(app, "ComposeFooterMessages");
                Check(Contains(contents, town.Notice) && Read<string>(contents, "firstLabel") == "ヒント", "failed-build notice is identified as guidance");
                Set(app, "message", "水辺の午後の絵はがきが届きました！");
                Set(app, "messageIsHint", false);
                town.Notice = "白い鳩『しらたま』がやってきました！";
                contents = Call(app, "ComposeFooterMessages");
                Check(Contains(contents, "水辺の午後の絵はがきが届きました！") && Contains(contents, town.Notice), "feedback and distinct town event both remain visible");
                town.Notice = "水辺の午後の絵はがきが届きました！";
                contents = Call(app, "ComposeFooterMessages");
                Check(Read<string>(contents, "secondText") == null, "matching feedback and event are shown once");
                Call(app, "ShowEvent", "水辺の午後の絵はがきが届きました！");
                contents = Call(app, "ComposeFooterMessages");
                Check(Read<string>(contents, "firstLabel") == "できごと", "postcard delivery is labeled as a town event");
                object layout = Call(app, "FooterRects", new Rect(300, 700, 840, 192), true);
                var left = Read<Rect>(layout, "leftBody");
                var right = Read<Rect>(layout, "rightBody");
                var buttons = Read<Rect>(layout, "buttons");
                Check(left.xMax < right.xMin && left.yMax < buttons.yMin && right.yMax < buttons.yMin, "two notice columns leave the management buttons clear");
                Check(left.yMin >= 700 && right.yMin >= 700 && buttons.yMax <= 892, "footer contents remain inside the fixed panel");
                string large = (string)Call(app, "CompactAmount", 12345678f);
                Check(large.Contains("万") || large.Contains("億"), "large amounts have a compact readable unit");
                Debug.Log("PIGEON STATUS PANEL VERIFICATION PASSED");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
