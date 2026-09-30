using System.Collections.Generic;
using UnityEngine;

namespace PigeonSandbox
{
    public sealed partial class SandboxApp
    {
        readonly List<string> recentNotices = new List<string>();
        string toastText = "", lastRecordedTownNotice = "";
        float toastUntil;
        int unreadNotices;

        void ShowToast(string value)
        {
            if (string.IsNullOrEmpty(value) || value.StartsWith("まちの会計："))
                return;
            toastText = value;
            toastUntil = UnityEngine.Time.realtimeSinceStartup + 3.6f;
            if (recentNotices.Count == 0 || recentNotices[0] != value)
            {
                recentNotices.Insert(0, value);
                if (overlay != OverlayPanel.Notices)
                    unreadNotices++;
            }
            if (recentNotices.Count > 12)
                recentNotices.RemoveAt(recentNotices.Count - 1);
        }

        void DrawMapFirstUi(UiLayout layout, float w, float h)
        {
            DrawCompactHeader(layout, w);
            if (overlay == OverlayPanel.None)
            {
                FocusMarker();
                CameraControls();
                DrawQuickActions(w, h);
                DrawToolDock(w, h);
                DrawMenuButton(w, h);
                DrawSelectionCard(w, h);
            }
            else
                DrawOverlay(w, h);
            DrawToast(w, layout.headerH);
        }

        void DrawCompactHeader(UiLayout layout, float w)
        {
            Panel(new Rect(0, 0, w, layout.headerH), paper);
            Panel(new Rect(0, layout.headerH - 1, w, 1), new Color(.81f, .82f, .74f));
            Label(18, 11, 245, 31, "鳩市長の街づくり", title);
            Label(20, 49, 242, 22, "鳩と人が暮らす小さな街", small);
            HeaderAmount(274, 11, 202, "予算 ¥", town.Money, budgetLine, 17);
            Label(275, 50, 200, 20, "収入 ¥" + CompactAmount(town.Income) + " · 維持 ¥" + CompactAmount(town.Upkeep), small);
            Meter(492, 17, 112, "鳩の幸福", town.PigeonHappiness);
            Meter(618, 17, 112, "人の満足", town.HumanSatisfaction);
            Label(746, 17, 168, 24, "住民 " + town.Birds.Count + "羽 / 来訪 " + town.Visitors.Count + "人", text);
            Label(746, 50, 168, 20, "食料 " + Mathf.RoundToInt(town.FoodSupply) + " · 清潔 " + Mathf.RoundToInt(town.Cleanliness), small);
            Label(930, 15, 246, 24, town.Year + "年目 " + town.SeasonName + town.SeasonDay + "日", text);
            Label(930, 48, 246, 22, "DAY " + town.Day + " · " + town.TimeOfDayName, small);
            float speedX = w - 18 - 102, pauseX = speedX - 110;
            if (Button(new Rect(pauseX, 18, 102, 46), paused ? "再開" : "一時停止", paused))
                paused = !paused;
            if (Button(new Rect(speedX, 18, 102, 46), "速度 ×" + speed))
                speed = speed >= 3 ? 1 : 3;
        }

        int PendingWishCount()
        {
            int count = 0;
            foreach (var wish in town.Wishes)
                if (!wish.Complete)
                    count++;
            foreach (var request in town.Requests)
                if (!request.Complete)
                    count++;
            if (town.WaterfrontUnlocked)
                foreach (var request in town.WaterfrontRequests)
                    if (!request.Complete)
                        count++;
            return count;
        }

        void DrawQuickActions(float w, float h)
        {
            Rect r = QuickActionsRect(w, h);
            float x = r.x + 4;
            if (Button(new Rect(x, r.y + 2, 110, 44), "お願い " + PendingWishCount(), overlay == OverlayPanel.Wishes))
                OpenOverlay(OverlayPanel.Wishes);
            string notices = unreadNotices > 0 ? "お知らせ " + Mathf.Min(99, unreadNotices) : "お知らせ";
            if (Button(new Rect(x + 116, r.y + 2, 110, 44), notices, overlay == OverlayPanel.Notices))
                OpenOverlay(OverlayPanel.Notices);
            if (Button(new Rect(x + 232, r.y + 2, 110, 44), "鳩図鑑", overlay == OverlayPanel.Codex))
                OpenOverlay(OverlayPanel.Codex);
        }

        void DrawToolDock(float w, float h)
        {
            Rect r = ToolDockRect(w, h);
            Panel(r, paper);
            float x = r.x + 8, y = r.y + 7;
            if (Button(new Rect(x, y, 120, 46), "＋ 建設", overlay == OverlayPanel.Build && catalogCategory != 3))
            {
                catalogCategory = 0;
                OpenOverlay(OverlayPanel.Build);
            }
            if (Button(new Rect(x + 128, y, 120, 46), "観察", tool == Tool.Inspect && selected < 0 && overlay == OverlayPanel.None))
            {
                CloseOverlay();
                tool = Tool.Inspect;
                selected = -1;
            }
            if (Button(new Rect(x + 256, y, 120, 46), "装飾", overlay == OverlayPanel.Build && catalogCategory == 3))
            {
                catalogCategory = 3;
                OpenOverlay(OverlayPanel.Build);
            }
        }

        void DrawMenuButton(float w, float h)
        {
            Rect r = MenuButtonRect(w, h);
            bool hit = Button(r, "", overlay == OverlayPanel.Menu);
            for (int i = 0; i < 3; i++)
                Panel(new Rect(r.x + 16, r.y + 19 + i * 7, 24, 2), overlay == OverlayPanel.Menu ? Color.white : green);
            if (hit)
                OpenOverlay(OverlayPanel.Menu);
        }

        void DrawSelectionCard(float w, float h)
        {
            if (overlay != OverlayPanel.None)
                return;
            var facility = town.Facilities.Find(f => f.Id == selected);
            if (tool == Tool.Build)
            {
                Rect r = SelectionRect(w, h);
                Panel(r, paper);
                Label(r.x + 16, r.y + 9, 440, 28, TownSimulation.NameOf(building) + "  ¥" + TownSimulation.Cost(building), heading);
                Label(r.x + 16, r.y + 46, 490, 30, "空き地をクリックして建設 · " + Tips[(int)building], small);
                if (Button(new Rect(r.xMax - 120, r.y + 24, 104, 44), "取消"))
                    tool = Tool.Inspect;
                return;
            }
            if (facility == null)
                return;
            Rect card = SelectionRect(w, h);
            Panel(card, paper);
            Label(card.x + 16, card.y + 8, 540, 30, TownSimulation.NameOf(facility.Kind) + "  Lv." + facility.Level, heading);
            Label(card.x + 16, card.y + 40, card.width - 32, 28, tool == Tool.Move ? "空き地をクリックして移設" : Tips[(int)facility.Kind], small);
            float by = card.yMax - 48;
            if (Button(new Rect(card.x + 16, by, 106, 40), "移設 ¥0", tool == Tool.Move))
                tool = Tool.Move;
            int cost = TownSimulation.Cost(facility.Kind) * facility.Level / 2;
            if (Button(new Rect(card.x + 130, by, 132, 40), facility.Level >= 3 ? "改良済み" : "改良 ¥" + cost, false, facility.Level < 3 && town.Money >= cost))
            {
                town.Upgrade(facility.Id);
                ShowToast(town.Notice);
            }
            if (Button(new Rect(card.x + card.width - 270, by, 146, 40), "撤去 / 70%返金"))
            {
                town.Remove(facility.Id);
                ShowToast(town.Notice);
                selected = -1;
                tool = Tool.Inspect;
            }
            if (Button(new Rect(card.xMax - 116, by, 100, 40), "閉じる"))
            {
                selected = -1;
                tool = Tool.Inspect;
            }
        }

        void DrawOverlay(float w, float h)
        {
            float header = Layout(w, h).headerH;
            Rect scrim = new Rect(0, header, w, h - header);
            Panel(scrim, new Color(.08f, .15f, .11f, .28f));
            if (overlay == OverlayPanel.Build)
                DrawBuildSheet(w, h);
            else if (overlay == OverlayPanel.Menu)
                DrawGameMenu(w, h);
            else if (overlay != OverlayPanel.None)
                DrawDetailDrawer(w, h);
        }

        void DrawBuildSheet(float w, float h)
        {
            float width = Mathf.Min(920, w - 36), height = 334;
            Rect r = new Rect((w - width) / 2, h - height, width, height);
            Panel(r, paper);
            Label(r.x + 20, r.y + 12, width - 160, 32, "建設する施設を選ぶ", heading);
            if (CloseButton(new Rect(r.xMax - 120, r.y + 10, 100, 40)))
            {
                CloseOverlay();
                return;
            }
            string[] categories = {"すべて", "食べ物", "暮らし", "自然・交流"};
            for (int i = 0; i < categories.Length; i++)
                if (Button(new Rect(r.x + 20 + i * 124, r.y + 58, 116, 36), categories[i], catalogCategory == i))
                {
                    catalogCategory = i;
                    return;
                }
            string[] uses = {"食料", "水浴び", "寝床", "休憩", "交流", "観光", "商業・交流", "緑地・休息"};
            float gap = 10, cardW = (width - 40 - gap * 3) / 4, cardH = 98;
            int shown = 0;
            foreach (FacilityKind kind in System.Enum.GetValues(typeof(FacilityKind)))
            {
                if (!FacilityInCategory(kind, catalogCategory))
                    continue;
                int column = shown % 4, row = shown / 4;
                Rect card = new Rect(r.x + 20 + column * (cardW + gap), r.y + 108 + row * (cardH + gap), cardW, cardH);
                if (FacilityCard(card, kind, uses[(int)kind]))
                    return;
                shown++;
            }
        }

        bool FacilityCard(Rect card, FacilityKind kind, string use)
        {
            bool hit = Button(card, "");
            Label(card.x + 14, card.y + 10, card.width - 28, 28, TownSimulation.NameOf(kind), text);
            Label(card.x + 14, card.y + 40, card.width - 28, 21, use, small);
            Label(card.x + 14, card.y + 68, card.width - 28, 23, "¥" + TownSimulation.Cost(kind), statusLabel);
            if (hit)
                SelectBuilding(kind);
            return hit;
        }

        void DrawDetailDrawer(float w, float h)
        {
            float width = Mathf.Min(400, w - 32), top = Layout(w, h).headerH;
            Rect r = new Rect(w - width, top, width, h - top);
            Panel(r, paper);
            string label = overlay == OverlayPanel.Wishes ? "鳩からのお願い" : overlay == OverlayPanel.Notices ? "街のお知らせ" : overlay == OverlayPanel.Codex ? "鳩図鑑" : overlay == OverlayPanel.Policies ? "条例" : "街の催し";
            Label(r.x + 18, r.y + 18, r.width - 150, 32, label, heading);
            if (CloseButton(new Rect(r.xMax - 126, r.y + 12, 108, 42)))
            {
                CloseOverlay();
                return;
            }
            float contentW = r.width - 52, contentY = r.y + 70, contentH = r.height - 86;
            if (overlay == OverlayPanel.Notices)
            {
                DrawNoticeList(new Rect(r.x + 18, contentY, r.width - 36, contentH));
                return;
            }
            tab = overlay == OverlayPanel.Wishes ? 0 : overlay == OverlayPanel.Codex ? 1 : overlay == OverlayPanel.Policies ? 2 : 3;
            float height = RightContent(contentW, false);
            scroll = GUI.BeginScrollView(new Rect(r.x + 18, contentY, r.width - 30, contentH), scroll, new Rect(0, 0, contentW, Mathf.Max(contentH, height)), false, true);
            RightContent(contentW, true);
            GUI.EndScrollView();
        }

        void DrawNoticeList(Rect area)
        {
            float width = area.width - 16, total = 12;
            foreach (string notice in recentNotices)
                total += TextHeight(notice, width - 20, text) + 34;
            scroll = GUI.BeginScrollView(area, scroll, new Rect(0, 0, width, Mathf.Max(area.height, total)), false, true);
            float y = 8;
            if (recentNotices.Count == 0)
                y = FlowLabel(10, y, width - 20, "街のできごとがここに届きます。", small);
            foreach (string notice in recentNotices)
            {
                float height = TextHeight(notice, width - 20, text) + 20;
                Panel(new Rect(4, y, width - 8, height), new Color(.9f, .91f, .85f));
                Label(14, y + 9, width - 28, height - 18, notice, text);
                y += height + 14;
            }
            GUI.EndScrollView();
        }

        void TryExpandTown()
        {
            if (!(town.CanExpand ? town.ExpandTown() : town.UnlockWaterfront()))
                return;
            if (town.WaterfrontUnlocked)
                FocusDistrict(true);
            else
                ResetCamera();
            world.Sync(town, paused);
            ShowFeedback(town.Notice);
            Save(false);
            CloseOverlay();
        }

        void DrawGameMenu(float w, float h)
        {
            float top = Layout(w, h).headerH, width = 350;
            Rect r = new Rect(0, top, width, h - top);
            Panel(r, paper);
            Label(20, top + 18, width - 160, 32, "ゲームメニュー", heading);
            if (CloseButton(new Rect(width - 126, top + 12, 108, 42)))
            {
                CloseOverlay();
                return;
            }
            float y = top + 74;
            if (Button(new Rect(20, y, width - 40, 48), "ゲームに戻る"))
            {
                CloseOverlay();
                return;
            }
            y += 58;
            if (Button(new Rect(20, y, width - 40, 48), "条例"))
            {
                OpenOverlay(OverlayPanel.Policies);
                return;
            }
            y += 58;
            if (Button(new Rect(20, y, width - 40, 48), town.FestivalDue && !town.FestivalActive ? "催し！" : "催し"))
            {
                OpenOverlay(OverlayPanel.Festivals);
                return;
            }
            y += 58;
            if (Button(new Rect(20, y, width - 40, 48), "街を保存"))
            {
                Save(true);
                CloseOverlay();
                return;
            }
            y += 58;
            string expansion = town.CanExpand ? "土地を広げる ¥" + town.ExpansionCost : town.WaterfrontUnlocked ? "水辺地区 開放済み" : "水辺地区を開く ¥" + TownSimulation.WaterfrontCost;
            bool affordable = town.CanExpand ? town.Money >= town.ExpansionCost : !town.WaterfrontUnlocked && town.Money >= TownSimulation.WaterfrontCost;
            if (Button(new Rect(20, y, width - 40, 52), expansion, false, affordable))
            {
                TryExpandTown();
                return;
            }
            y += 70;
            Label(20, y, width - 40, 22, "マップ操作", statusLabel);
            Label(20, y + 28, width - 40, 74, "ドラッグで移動 · ホイールでズーム\n右ドラッグで視点回転", small);
        }

        void DrawToast(float w, float header)
        {
            if (toastUntil <= UnityEngine.Time.realtimeSinceStartup)
                return;
            float width = Mathf.Min(540, w - 40), x = (w - width) / 2;
            float height = Mathf.Clamp(TextHeight(toastText, width - 32, toastStyle) + 18, 48, 112);
            Rect r = new Rect(x, header + 72, width, height);
            Panel(r, green);
            Label(x + 16, r.y + 9, width - 32, height - 18, toastText, toastStyle);
        }
    }
}
