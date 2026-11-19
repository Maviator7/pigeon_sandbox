using System.Collections.Generic;
using UnityEngine;

namespace PigeonSandbox
{
    public sealed partial class SandboxApp
    {
        static readonly FacilityKind[] FacilityKinds = (FacilityKind[])System.Enum.GetValues(typeof(FacilityKind));
        static readonly Color[] FacilityAccents = {new Color(.72f, .43f, .29f), new Color(.34f, .58f, .68f), new Color(.59f, .48f, .38f), new Color(.35f, .55f, .37f), new Color(.67f, .59f, .39f), new Color(.71f, .55f, .32f), new Color(.72f, .5f, .35f), new Color(.42f, .59f, .39f)};
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
                DrawCommandRail(w, h);
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
            Color divider = new Color(.81f, .82f, .75f);
            Panel(new Rect(263, 15, 1, 52), divider);
            Panel(new Rect(482, 15, 1, 52), divider);
            Panel(new Rect(734, 15, 1, 52), divider);
            Panel(new Rect(920, 15, 1, 52), divider);
            Panel(new Rect(w - 244, 15, 1, 52), divider);
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
            Surface(r, paper);
            float x = r.x + 4;
            if (Button(new Rect(x, r.y + 2, 110, 44), "お願い " + PendingWishCount(), overlay == OverlayPanel.Wishes))
                OpenOverlay(OverlayPanel.Wishes);
            string notices = unreadNotices > 0 ? "お知らせ " + Mathf.Min(99, unreadNotices) : "お知らせ";
            if (Button(new Rect(x + 116, r.y + 2, 110, 44), notices, overlay == OverlayPanel.Notices))
                OpenOverlay(OverlayPanel.Notices);
            if (Button(new Rect(x + 232, r.y + 2, 110, 44), "鳩図鑑", overlay == OverlayPanel.Codex))
                OpenOverlay(OverlayPanel.Codex);
        }

        void DrawCommandRail(float w, float h)
        {
            Rect r = CommandRailRect(w, h);
            Surface(r, paper);
            float x = r.x + 8, y = r.y + 8;
            if (CommandButton(new Rect(x, y, 68, 58), "＋ 建設", 0, (tool == Tool.Build || overlay == OverlayPanel.Build) && catalogCategory != 3))
            {
                catalogCategory = 0;
                OpenOverlay(OverlayPanel.Build);
            }

            if (CommandButton(new Rect(x, y + 66, 68, 58), "観察", 1, tool == Tool.Inspect && overlay == OverlayPanel.None))
            {
                CloseOverlay();
                tool = Tool.Inspect;
                selected = -1;
            }

            if (CommandButton(new Rect(x, y + 132, 68, 58), "装飾", 2, (tool == Tool.Build || overlay == OverlayPanel.Build) && catalogCategory == 3))
            {
                catalogCategory = 3;
                OpenOverlay(OverlayPanel.Build);
            }

            Panel(new Rect(x + 14, y + 196, 40, 1), new Color(.81f, .82f, .75f));
            if (CommandButton(new Rect(x, y + 198, 68, 58), "", 3, overlay == OverlayPanel.Menu))
                OpenOverlay(OverlayPanel.Menu);
        }

        bool CommandButton(Rect r, string label, int icon, bool active)
        {
            bool hit = Button(r, "", active);
            Color foreground = active ? Color.white : green;
            Rect mark = new Rect(r.x + (r.width - 22) / 2, r.y + 7, 22, 20);
            if (icon == 0)
            {
                Panel(new Rect(mark.x + 2, mark.y + 9, 18, 8), foreground);
                Panel(new Rect(mark.x + 4, mark.y + 4, 14, 6), foreground);
                Panel(new Rect(mark.x + 7, mark.y, 2, 20), active ? green : paper);
                Panel(new Rect(mark.x + 15, mark.y, 2, 20), active ? green : paper);
            }
            else if (icon == 1)
            {
                Disc(new Rect(mark.x + 2, mark.y + 1, 18, 18), foreground);
                Disc(new Rect(mark.x + 7, mark.y + 6, 8, 8), active ? green : paper);
                Disc(new Rect(mark.x + 9, mark.y + 8, 4, 4), foreground);
            }
            else if (icon == 2)
            {
                Panel(new Rect(mark.x + 9, mark.y + 11, 4, 9), foreground);
                Disc(new Rect(mark.x + 2, mark.y + 1, 12, 12), foreground);
                Disc(new Rect(mark.x + 9, mark.y + 1, 12, 12), foreground);
                Disc(new Rect(mark.x + 6, mark.y + 5, 12, 12), active ? green : paper);
            }
            else
                for (int i = 0; i < 3; i++)
                    Panel(new Rect(mark.x + 1, mark.y + 2 + i * 7, 20, 2), foreground);
            if (!string.IsNullOrEmpty(label))
                Label(r.x + 2, r.y + 31, r.width - 4, 20, label, active ? commandActiveLabel : commandLabel);
            return hit;
        }

        void DrawSelectionCard(float w, float h)
        {
            if (overlay != OverlayPanel.None)
                return;
            var facility = town.Facilities.Find(f => f.Id == selected);
            if (tool == Tool.Build)
            {
                Rect r = SelectionRect(w, h);
                Surface(r, paper);
                Label(r.x + 16, r.y + 9, 440, 28, TownSimulation.NameOf(building) + "  ¥" + TownSimulation.Cost(building), heading);
                Label(r.x + 16, r.y + 46, 490, 30, "空き地をクリックして建設 · " + Tips[(int)building], small);
                if (Button(new Rect(r.xMax - 120, r.y + 24, 104, 44), "取消"))
                    tool = Tool.Inspect;
                return;
            }

            if (facility == null)
                return;
            Rect card = SelectionRect(w, h);
            Surface(card, paper);
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

            if (DangerButton(new Rect(card.x + card.width - 270, by, 146, 40), "撤去 / 70%返金"))
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
            Matrix4x4 matrix = GUI.matrix;
            Color color = GUI.color;
            float progress = Mathf.Clamp01((UnityEngine.Time.unscaledTime - overlayOpenedAt) / .18f);
            progress = progress * progress * (3 - 2 * progress);
            GUI.matrix = matrix * Matrix4x4.Translate(new Vector3(0, (1 - progress) * 10, 0));
            GUI.color = new Color(color.r, color.g, color.b, color.a * progress);
            Panel(scrim, new Color(.08f, .15f, .11f, .28f));
            if (overlay == OverlayPanel.Build)
                DrawBuildSheet(w, h);
            else if (overlay == OverlayPanel.Menu)
                DrawGameMenu(w, h);
            else if (overlay != OverlayPanel.None)
                DrawDetailDrawer(w, h);
            GUI.matrix = matrix;
            GUI.color = color;
        }

        void DrawBuildSheet(float w, float h)
        {
            float width = Mathf.Min(920, w - 36), height = 334;
            Rect r = new Rect((w - width) / 2, h - height, width, height);
            Surface(r, paper);
            Label(r.x + 20, r.y + 12, width - 160, 32, "建設する施設を選ぶ", heading);
            if (CloseButton(new Rect(r.xMax - 120, r.y + 10, 100, 40)))
            {
                CloseOverlay();
                return;
            }

            string[] categories = {"すべて", "食べ物", "暮らし", "自然・交流"};
            float tabWidth = 116, tabGap = 8;
            float tabX = r.x + (width - (tabWidth * categories.Length + tabGap * (categories.Length - 1))) / 2;
            for (int i = 0; i < categories.Length; i++)
                if (Button(new Rect(tabX + i * (tabWidth + tabGap), r.y + 58, tabWidth, 36), categories[i], catalogCategory == i))
                {
                    catalogCategory = i;
                    return;
                }

            string[] uses = {"食料", "水浴び", "寝床", "休憩", "交流", "観光", "商業・交流", "緑地・休息"};
            float gap = 10, cardW = (width - 40 - gap * 3) / 4, cardH = 98;
            int total = 0;
            foreach (FacilityKind kind in FacilityKinds)
                if (FacilityInCategory(kind, catalogCategory))
                    total++;
            int shown = 0;
            foreach (FacilityKind kind in FacilityKinds)
            {
                if (!FacilityInCategory(kind, catalogCategory))
                    continue;
                int column = shown % 4, row = shown / 4;
                int cardsInRow = Mathf.Min(4, total - row * 4);
                float rowWidth = cardsInRow * cardW + (cardsInRow - 1) * gap;
                float rowOffset = (width - 40 - rowWidth) / 2;
                Rect card = new Rect(r.x + 20 + rowOffset + column * (cardW + gap), r.y + 108 + row * (cardH + gap), cardW, cardH);
                if (FacilityCard(card, kind, uses[(int)kind]))
                    return;
                shown++;
            }
        }

        bool FacilityCard(Rect card, FacilityKind kind, string use)
        {
            bool hit = Button(card, "");
            Rect icon = new Rect(card.x + 12, card.y + 22, 52, 52);
            DrawFacilityPictogram(icon, kind);
            float textX = card.x + 74, textWidth = card.width - 88;
            Label(textX, card.y + 10, textWidth, 26, TownSimulation.NameOf(kind), text);
            Label(textX, card.y + 39, textWidth, 21, use, small);
            Label(textX, card.y + 68, textWidth, 23, "¥" + TownSimulation.Cost(kind), statusLabel);
            if (hit)
                SelectBuilding(kind);
            return hit;
        }

        void DrawFacilityPictogram(Rect r, FacilityKind kind)
        {
            int accentIndex = (int)kind;
            Color accent = accentIndex >= 0 && accentIndex < FacilityAccents.Length ? FacilityAccents[accentIndex] : green;
            Surface(r, new Color(.93f, .92f, .86f));
            Rect body;
            if (kind == FacilityKind.Bakery)
            {
                body = new Rect(r.x + 11, r.y + 22, 30, 20);
                Panel(body, accent);
                Panel(new Rect(r.x + 9, r.y + 17, 34, 6), new Color(.89f, .73f, .48f));
                Panel(new Rect(r.x + 14, r.y + 25, 6, 8), new Color(.96f, .88f, .68f));
                Panel(new Rect(r.x + 30, r.y + 26, 7, 16), new Color(.43f, .34f, .27f));
            }
            else if (kind == FacilityKind.Fountain)
            {
                Disc(new Rect(r.x + 10, r.y + 19, 32, 23), accent);
                Disc(new Rect(r.x + 15, r.y + 22, 22, 14), new Color(.93f, .92f, .86f));
                Panel(new Rect(r.x + 24, r.y + 10, 4, 16), accent);
                Disc(new Rect(r.x + 21, r.y + 7, 10, 10), new Color(.55f, .76f, .78f));
            }
            else if (kind == FacilityKind.Housing)
            {
                Panel(new Rect(r.x + 13, r.y + 19, 26, 24), accent);
                Panel(new Rect(r.x + 18, r.y + 13, 16, 7), new Color(.53f, .37f, .29f));
                Panel(new Rect(r.x + 17, r.y + 24, 6, 6), new Color(.96f, .88f, .68f));
                Panel(new Rect(r.x + 29, r.y + 24, 6, 6), new Color(.96f, .88f, .68f));
                Panel(new Rect(r.x + 24, r.y + 34, 5, 9), new Color(.43f, .34f, .27f));
            }
            else if (kind == FacilityKind.Tree)
            {
                Panel(new Rect(r.x + 23, r.y + 27, 7, 16), new Color(.49f, .35f, .25f));
                Disc(new Rect(r.x + 11, r.y + 10, 22, 24), accent);
                Disc(new Rect(r.x + 22, r.y + 15, 20, 20), new Color(.48f, .65f, .4f));
            }
            else if (kind == FacilityKind.Plaza)
            {
                Panel(new Rect(r.x + 11, r.y + 14, 13, 13), accent);
                Panel(new Rect(r.x + 28, r.y + 14, 13, 13), new Color(.83f, .7f, .48f));
                Panel(new Rect(r.x + 11, r.y + 31, 13, 13), new Color(.83f, .7f, .48f));
                Panel(new Rect(r.x + 28, r.y + 31, 13, 13), accent);
            }
            else if (kind == FacilityKind.ClockTower)
            {
                body = new Rect(r.x + 18, r.y + 12, 17, 32);
                Panel(body, accent);
                Disc(new Rect(r.x + 21, r.y + 16, 11, 11), new Color(.96f, .88f, .68f));
                Panel(new Rect(r.x + 26, r.y + 18, 1, 4), new Color(.43f, .34f, .27f));
                Panel(new Rect(r.x + 26, r.y + 22, 4, 1), new Color(.43f, .34f, .27f));
            }
            else if (kind == FacilityKind.Cafe)
            {
                Panel(new Rect(r.x + 12, r.y + 25, 28, 5), accent);
                Panel(new Rect(r.x + 16, r.y + 30, 3, 12), new Color(.49f, .35f, .25f));
                Panel(new Rect(r.x + 33, r.y + 30, 3, 12), new Color(.49f, .35f, .25f));
                Disc(new Rect(r.x + 29, r.y + 17, 8, 8), new Color(.88f, .73f, .48f));
            }
            else
            {
                Panel(new Rect(r.x + 24, r.y + 27, 5, 15), new Color(.49f, .35f, .25f));
                Disc(new Rect(r.x + 11, r.y + 9, 22, 23), accent);
                Disc(new Rect(r.x + 24, r.y + 15, 16, 18), new Color(.51f, .66f, .42f));
                Disc(new Rect(r.x + 12, r.y + 34, 6, 6), new Color(.86f, .62f, .61f));
                Disc(new Rect(r.x + 36, r.y + 35, 6, 6), new Color(.88f, .73f, .48f));
            }
        }

        void DrawDetailDrawer(float w, float h)
        {
            float width = Mathf.Min(400, w - 32), top = Layout(w, h).headerH;
            Rect r = new Rect(w - width, top, width, h - top);
            Surface(r, paper);
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
                Surface(new Rect(4, y, width - 8, height), new Color(.93f, .92f, .86f));
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
            Surface(r, paper);
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
