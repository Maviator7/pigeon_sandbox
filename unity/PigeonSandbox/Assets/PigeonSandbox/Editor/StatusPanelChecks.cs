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
                int originalUnread = (int)typeof(SandboxApp).GetField("unreadNotices", Private).GetValue(app);
                Call(app, "ShowFeedback", "新しい鳩が引っ越してきました！");
                Check((string)typeof(SandboxApp).GetField("toastText", Private).GetValue(app) == "新しい鳩が引っ越してきました！", "town feedback becomes a toast");
                Call(app, "ShowFeedback", "まちの会計：収入 47 / 維持費 88.0");
                Check((string)typeof(SandboxApp).GetField("toastText", Private).GetValue(app) == "新しい鳩が引っ越してきました！", "accounting does not replace a useful toast");
                Call(app, "ShowEvent", "水辺の午後の絵はがきが届きました！");
                Check((string)typeof(SandboxApp).GetField("toastText", Private).GetValue(app) == "水辺の午後の絵はがきが届きました！", "postcards remain visible as a toast");
                Check((int)typeof(SandboxApp).GetField("unreadNotices", Private).GetValue(app) == originalUnread + 2, "new announcements are counted once");
                string large = (string)Call(app, "CompactAmount", 12345678f);
                Check(large.Contains("万") || large.Contains("億"), "large amounts have a compact readable unit");
                string extreme = (string)Call(app, "CompactAmount", float.MaxValue);
                Check(extreme.Length < 16, "extreme finite budget values stay bounded");
                object mapLayout = Call(app, "Layout", 1440f, 900f);
                var map = Read<Rect>(mapLayout, "center");
                Check(map.width * map.height >= 1440f * 900f * .8f, "the town occupies at least eighty percent of a desktop window");
                Check(!(bool)Call(app, "UiBlocksMap", new Vector2(720, 450), 1440f, 900f), "uncovered town remains interactive");
                Check((bool)Call(app, "UiBlocksMap", new Vector2(720, 850), 1440f, 900f), "bottom toolbar blocks construction clicks");
                Check((bool)Call(app, "UiBlocksMap", new Vector2(1380, 118), 1440f, 900f), "quick actions block town clicks");
                foreach (FacilityKind kind in Enum.GetValues(typeof(FacilityKind)))
                    Check((bool)Call(app, "FacilityInCategory", kind, 0), "build catalog includes " + kind);
                Check((bool)Call(app, "FacilityInCategory", FacilityKind.Cafe, 1), "cafe remains available under food");
                Check((bool)Call(app, "FacilityInCategory", FacilityKind.Park, 3), "park remains available under nature");
                var overlayField = typeof(SandboxApp).GetField("overlay", Private);
                object notices = Enum.Parse(overlayField.FieldType, "Notices");
                Call(app, "OpenOverlay", notices);
                Check((int)typeof(SandboxApp).GetField("unreadNotices", Private).GetValue(app) == 0, "opening announcements marks them read");
                Call(app, "CloseOverlay");
                object wishes = Enum.Parse(overlayField.FieldType, "Wishes");
                Call(app, "OpenOverlay", wishes);
                Check((bool)Call(app, "UiBlocksMap", new Vector2(720, 450), 1440f, 900f), "an open drawer blocks map construction");
                Call(app, "CloseOverlay");
                Check(!(bool)Call(app, "UiBlocksMap", new Vector2(720, 450), 1440f, 900f), "closing a drawer restores map interaction");
                foreach (float width in new[] {1440f, 1600f, 1920f})
                {
                    Rect dock = (Rect)Call(app, "ToolDockRect", width, 900f);
                    Rect actions = (Rect)Call(app, "QuickActionsRect", width, 900f);
                    Rect menu = (Rect)Call(app, "MenuButtonRect", width, 900f);
                    Check(dock.x >= 0 && dock.xMax <= width && actions.x >= 0 && actions.xMax <= width && menu.x >= 0 && menu.xMax <= width,
                        "desktop controls stay inside a " + width + "px window");
                }
                Debug.Log("PIGEON STATUS PANEL VERIFICATION PASSED");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
