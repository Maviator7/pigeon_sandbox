using System;
using System.IO;
using UnityEngine;

namespace PigeonSandbox
{
    public sealed class SandboxApp : MonoBehaviour
    {
        // Set by the generated Benchmark scene: a full, unsaved town with the performance overlay and runner.
        [SerializeField]
        bool benchmark;
        // -benchmark-no-ui measures the world alone; the IMGUI panels are due to be rebuilt.
        bool hideUi;
        TownSimulation town;
        TownWorld world;
        Camera view;
        Font font;
        Light sun;
        GUIStyle title, heading, text, small, button, marker, nameInput;
        float yaw = 35, pitch = 48, zoom = 14, saveClock;
        int speed = 1, selected = -1, tab;
        bool paused;
        int postcardCount;
        int postcardPage;
        int seasonalPostcardCount, seasonalPostcardPage;
        TownSeason displayedSeason;
        float seasonToastUntil;
        FacilityKind building = FacilityKind.Bakery;
        enum Tool
        {
            Inspect,
            Build,
            Move
        }

        Tool tool = Tool.Build;
        Vector2 pointerStart, scroll, lastPointer, buildScroll;
        Vector3 cameraFocus = new Vector3(0, .3f, 0);
        int followedBird = -1;
        int editingBird = -1;
        string nameDraft = "", nameError = "";
        bool focusNameInput;
        IMECompositionMode previousImeMode;
        float targetZoom = 14;
        bool pointerInTown, dragged, multiTouch;
        string message = "広場のそばにベーカリーを建ててみましょう。";
        float Scale => Mathf.Min(Screen.width / 1440f, Screen.height / 900f);
        float Width => Screen.width / Scale;
        float Height => Screen.height / Scale;
        string SavePath => Path.Combine(Application.persistentDataPath, "mayor-town.json");
        readonly Color ink = new Color(.12f, .2f, .16f), muted = new Color(.22f, .29f, .24f), green = new Color(.27f, .41f, .33f), paper = new Color(.96f, .95f, .9f);
        static readonly string[] Tips = {"広場から2マス以内で混雑を軽減。噴水を添えると鳩の生活圏に。", "パン屋の2マス以内で食事と水浴びを楽しめる場所に。", "木や噴水が近く、店舗から離れた場所なら静かな寝床に。", "住宅のそばで静かな緑地に。清掃の負担もやわらげます。", "パン屋に近づけると混雑をやわらげ、人と鳩の居場所を確保。", "観光と目立ちたがりの鳩のための名所。静かな住宅からは距離を。", "パン屋の2マス以内で売上と食料供給がアップ。テラスで人と鳩がひと休み。", "住宅の2マス以内で静かな居場所に。混雑をやわらげ、日向ぼっこや羽繕いを楽しめます。"};
        struct UiLayout
        {
            public float gap, headerH, bottomH, leftW, rightW;
            public Rect left, right, center, bottom;
        }

        UiLayout Layout(float w, float h)
        {
            float gap = Mathf.Clamp(w * .012f, 14, 20), headerH = Mathf.Clamp(h * .12f, 104, 116), bottomH = 192;
            float leftW = Mathf.Clamp(w * .18f, 248, 276), rightW = Mathf.Clamp(w * .205f, 286, 320);
            float centerX = leftW + gap, centerY = headerH + gap;
            float centerW = Mathf.Max(480, w - leftW - rightW - gap * 2), centerH = Mathf.Max(260, h - headerH - bottomH - gap * 2);
            return new UiLayout{gap = gap, headerH = headerH, bottomH = bottomH, leftW = leftW, rightW = rightW, left = new Rect(0, headerH, leftW, h - headerH), right = new Rect(w - rightW, headerH, rightW, h - headerH), center = new Rect(centerX, centerY, centerW, centerH), bottom = new Rect(centerX, h - bottomH, centerW, bottomH)};
        }

        void Start()
        {
            Application.targetFrameRate = 60;
            font = Resources.Load<Font>("NotoSansJP");
            if (benchmark)
            {
                town = TownSimulation.CreateBenchmark();
                hideUi = Array.IndexOf(Environment.GetCommandLineArgs(), "-benchmark-no-ui") >= 0;
                foreach (string arg in Environment.GetCommandLineArgs())
                    if (arg.StartsWith("-benchmark-day=", StringComparison.Ordinal) && int.TryParse(arg.Substring(15), out int day) && day >= 1)
                    {
                        town.Day = day;
                        town.Time = (day - 1) * TownSimulation.DayLength;
                    }

                if (Array.IndexOf(Environment.GetCommandLineArgs(), "-benchmark-boundary") >= 0)
                    town.Time += TownSimulation.DayLength - 7;
                message = "ベンチマーク用の街です。変更は保存されません。";
            }
            else
            {
                town = new TownSimulation();
                Load();
            }

            postcardCount = town.Postcards.Count;
            seasonalPostcardCount = town.SeasonalPostcards.Count;
            displayedSeason = town.Season;
            zoom = targetZoom = 14 + town.ExpansionLevel * 3;
            world = new GameObject("Mayor town").AddComponent<TownWorld>();
            world.Initialize();
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.72f, .75f, .7f);
            sun = new GameObject("Daylight").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = .85f;
            sun.color = new Color(1, .95f, .84f);
            sun.shadows = Application.isMobilePlatform ? LightShadows.Hard : LightShadows.Soft;
            sun.shadowStrength = .55f;
            sun.transform.rotation = Quaternion.Euler(52, -35, 0);
            if (Application.isMobilePlatform)
            {
                // One cascade at medium resolution keeps the shadow pass affordable on phones.
                QualitySettings.shadowCascades = 1;
                QualitySettings.shadowResolution = ShadowResolution.Medium;
            }

            view = new GameObject("Town camera").AddComponent<Camera>();
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(.76f, .8f, .71f);
            view.orthographic = true;
            view.nearClipPlane = .1f;
            view.farClipPlane = 100;
            view.gameObject.AddComponent<AudioListener>();
            CameraPosition();
            UpdateDaylight();
            world.Sync(town, false);
            var overlay = gameObject.AddComponent<PerformanceOverlay>();
            if (benchmark)
                gameObject.AddComponent<BenchmarkRunner>().Begin(town, overlay);
        }

        void UpdateDaylight()
        {
            if (sun == null || view == null)
                return;
            // Three daylight anchors form a continuous loop, including evening back to morning.
            float phase = town.DayProgress * 3;
            int from = Mathf.Min(2, (int)phase), to = (from + 1) % 3;
            float blend = Mathf.SmoothStep(0, 1, phase - from);
            int season = (int)town.Season;
            sun.color = Color.Lerp(Color.Lerp(DaylightSun[from], DaylightSun[to], blend), SeasonalSun[season], .17f);
            sun.intensity = Mathf.Lerp(DaylightIntensity[from], DaylightIntensity[to], blend);
            RenderSettings.ambientLight = Color.Lerp(Color.Lerp(DaylightAmbient[from], DaylightAmbient[to], blend), SeasonalAmbient[season], .24f);
            view.backgroundColor = Color.Lerp(Color.Lerp(DaylightSky[from], DaylightSky[to], blend), SeasonalSky[season], .3f);
        }

        static readonly Color[] DaylightSun = {new Color(1, .94f, .81f), new Color(1, .98f, .92f), new Color(1, .77f, .59f)};
        static readonly Color[] DaylightAmbient = {new Color(.72f, .75f, .7f), new Color(.76f, .79f, .74f), new Color(.75f, .7f, .65f)};
        static readonly Color[] DaylightSky = {new Color(.76f, .8f, .71f), new Color(.76f, .82f, .77f), new Color(.85f, .75f, .66f)};
        static readonly float[] DaylightIntensity = {.82f, .9f, .76f};
        static readonly Color[] SeasonalSun = {new Color(1, .93f, .87f), new Color(1, .98f, .85f), new Color(1, .81f, .66f), new Color(.85f, .91f, 1)};
        static readonly Color[] SeasonalAmbient = {new Color(.78f, .78f, .73f), new Color(.72f, .8f, .74f), new Color(.8f, .73f, .64f), new Color(.73f, .77f, .79f)};
        static readonly Color[] SeasonalSky = {new Color(.83f, .83f, .78f), new Color(.72f, .83f, .79f), new Color(.85f, .75f, .65f), new Color(.83f, .86f, .87f)};
        void CameraPosition()
        {
            var l = Layout(Width, Height);
            view.rect = new Rect(l.center.x / Width, (Height - l.center.y - l.center.height) / Height, l.center.width / Width, l.center.height / Height);
            var focus = cameraFocus;
            view.transform.position = focus + Quaternion.Euler(pitch, yaw, 0) * new Vector3(0, 0, -35);
            view.transform.LookAt(focus);
            view.orthographicSize = zoom;
            // Shadows only need to reach what the orthographic view shows: equal to the old fixed 70 at the full-map overview, tighter when zoomed in.
            QualitySettings.shadowDistance = 40 + zoom * 1.15f;
        }

        Rect CameraToolbar()
        {
            var r = Layout(Width, Height).center;
            return new Rect(r.x + 10, r.y + 10, r.width - 20, 48);
        }

        bool InTown(Vector2 point)
        {
            var p = point / Scale;
            p.y = Height - p.y;
            return Layout(Width, Height).center.Contains(p) && !CameraToolbar().Contains(p);
        }

        void FocusBird(int id)
        {
            followedBird = id;
            targetZoom = 4;
            tool = Tool.Inspect;
            selected = -1;
        }

        void StopFollowing()
        {
            followedBird = -1;
        }

        void SelectBuilding(FacilityKind kind)
        {
            StopFollowing();
            building = kind;
            tool = Tool.Build;
            selected = -1;
            message = Tips[(int)kind];
        }

        void BeginRename(TownBird bird)
        {
            if (editingBird < 0)
                previousImeMode = Input.imeCompositionMode;
            editingBird = bird.Id;
            nameDraft = bird.Name;
            nameError = "";
            focusNameInput = true;
            Input.imeCompositionMode = IMECompositionMode.On;
        }

        void CancelRename()
        {
            if (editingBird >= 0)
                Input.imeCompositionMode = previousImeMode;
            editingBird = -1;
            nameError = "";
            focusNameInput = false;
        }

        void ConfirmRename()
        {
            if (!string.IsNullOrEmpty(Input.compositionString))
                return;
            // The shipped font is a subset (kana, ASCII, Jōyō kanji and game text); refuse names it cannot draw.
            if (!town.RenameBird(editingBird, nameDraft, ch => font != null && font.HasCharacter(ch)))
            {
                nameError = "1〜24文字で入力してください。絵文字や一部の漢字、改行は使えません。";
                return;
            }

            string newName = town.Birds.Find(b => b.Id == editingBird).Name;
            CancelRename();
            message = "名前を「" + newName + "」に変更しました。";
            Save(false);
        }

        float RenameControls(TownBird bird, float y, float width, bool draw)
        {
            if (editingBird != bird.Id)
                return y;
            y = FlowLabel(0, y, width, "「" + bird.Name + "」の名前を変更", small, draw);
            if (draw)
            {
                GUI.SetNextControlName("bird-name-input");
                nameDraft = GUI.TextField(new Rect(0, y, width, 44), nameDraft, 192, nameInput);
                if (focusNameInput)
                {
                    GUI.FocusControl("bird-name-input");
                    focusNameInput = false;
                }
            }

            y += 50;
            y = FlowLabel(0, y, width, "名前は1〜24文字（かな・常用漢字・英数字など） · 同じ名前もOK", small, draw);
            float half = (width - 8) / 2;
            if (draw && Button(new Rect(0, y, half, 40), "保存", true))
                ConfirmRename();
            if (draw && Button(new Rect(half + 8, y, half, 40), "キャンセル"))
            {
                CancelRename();
                GUI.FocusControl(null);
            }

            y += 50;
            if (!string.IsNullOrEmpty(nameError))
                y = FlowLabel(0, y, width, nameError, small, draw);
            return y + 8;
        }

        void ResetCamera()
        {
            StopFollowing();
            cameraFocus = new Vector3(0, .3f, 0);
            targetZoom = 14 + town.ExpansionLevel * 3;
            yaw = 35;
            pitch = 48;
        }

        void PanCamera(Vector2 from, Vector2 to)
        {
            var plane = new Plane(Vector3.up, new Vector3(0, cameraFocus.y, 0));
            var a = view.ScreenPointToRay(from);
            var b = view.ScreenPointToRay(to);
            if (!plane.Raycast(a, out float da) || !plane.Raycast(b, out float db))
                return;
            followedBird = -1;
            cameraFocus += a.GetPoint(da) - b.GetPoint(db);
            cameraFocus.x = Mathf.Clamp(cameraFocus.x, -town.MapEdge - 4, town.MapEdge + 4);
            cameraFocus.z = Mathf.Clamp(cameraFocus.z, -town.MapEdge - 4, town.MapEdge + 4);
        }

        void CameraInput()
        {
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began)
                {
                    pointerStart = lastPointer = t.position;
                    pointerInTown = InTown(t.position);
                    dragged = false;
                    if (Input.touchCount == 1)
                        multiTouch = false;
                }

                if (Input.touchCount >= 2)
                {
                    multiTouch = true;
                    dragged = true;
                    var b = Input.GetTouch(1);
                    if (pointerInTown && InTown(b.position) && (t.phase == TouchPhase.Moved || b.phase == TouchPhase.Moved) && b.phase != TouchPhase.Began)
                    {
                        var oldA = t.position - t.deltaPosition;
                        var oldB = b.position - b.deltaPosition;
                        targetZoom = Mathf.Clamp(targetZoom + (Vector2.Distance(oldA, oldB) - Vector2.Distance(t.position, b.position)) * .015f, 3, 32);
                        PanCamera((oldA + oldB) * .5f, (t.position + b.position) * .5f);
                    }
                }
                else if (pointerInTown && !multiTouch)
                {
                    if (Vector2.Distance(pointerStart, t.position) > 7 * Scale)
                        dragged = true;
                    if (dragged && t.phase == TouchPhase.Moved)
                        PanCamera(lastPointer, t.position);
                    if (t.phase == TouchPhase.Ended && !dragged)
                        ClickTown(t.position);
                }

                lastPointer = t.position;
                if (t.phase == TouchPhase.Canceled || t.phase == TouchPhase.Ended)
                    pointerInTown = false;
                return;
            }

            Vector2 mouse = Input.mousePosition;
            if (Input.GetMouseButtonDown(0))
            {
                pointerStart = lastPointer = mouse;
                pointerInTown = InTown(mouse);
                dragged = false;
            }

            if (Input.GetMouseButton(0) && pointerInTown)
            {
                if (Vector2.Distance(pointerStart, mouse) > 7 * Scale)
                    dragged = true;
                if (dragged)
                    PanCamera(lastPointer, mouse);
            }

            if (Input.GetMouseButtonUp(0))
            {
                dragged |= Vector2.Distance(pointerStart, mouse) > 7 * Scale;
                if (pointerInTown && !dragged && InTown(mouse))
                    ClickTown(mouse);
                pointerInTown = false;
                dragged = false;
            }

            if (InTown(mouse))
            {
                if (Input.GetMouseButton(1))
                {
                    yaw += Input.GetAxis("Mouse X") * 3;
                    pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 2, 30, 75);
                }

                targetZoom = Mathf.Clamp(targetZoom - Input.mouseScrollDelta.y * .55f, 3, 32);
            }

            lastPointer = mouse;
        }

        void UpdateCamera(float deltaTime)
        {
            float blend = 1 - Mathf.Exp(-8 * Mathf.Max(0, deltaTime));
            var bird = town.BirdById(followedBird);
            if (bird != null)
                cameraFocus = Vector3.Lerp(cameraFocus, new Vector3(bird.X, bird.Y + .4f, bird.Z), blend);
            else
                followedBird = -1;
            zoom = Mathf.Lerp(zoom, targetZoom, blend);
            CameraPosition();
        }

        void CameraControls()
        {
            Rect r = CameraToolbar();
            Panel(r, paper);
            float x = r.x + 4, y = r.y + 2;
            if (Button(new Rect(x, y, 44, 44), "＋"))
                targetZoom = Mathf.Max(3, targetZoom - 2);
            if (Button(new Rect(x + 50, y, 44, 44), "−"))
                targetZoom = Mathf.Min(32, targetZoom + 2);
            if (Button(new Rect(x + 100, y, 96, 44), "街全体"))
                ResetCamera();
            var bird = town.Birds.Find(b => b.Id == followedBird);
            string caption = bird == null ? "ドラッグで移動" : bird.Name + "を追従中";
            float captionWidth = r.width - 220 - (bird != null ? 104 : 0);
            GUI.Label(new Rect(x + 208, y + 9, captionWidth, 34), caption, small);
            if (bird != null && Button(new Rect(r.xMax - 100, y, 96, 44), "追従解除"))
                StopFollowing();
        }

        void FocusMarker()
        {
            var bird = town.Birds.Find(b => b.Id == followedBird);
            if (bird == null)
                return;
            Vector3 screen = view.WorldToScreenPoint(new Vector3(bird.X, bird.Y + .7f, bird.Z));
            if (screen.z <= 0)
                return;
            var map = Layout(Width, Height).center;
            Vector2 point = new Vector2(screen.x / Scale, Height - screen.y / Scale);
            if (!map.Contains(point) || point.y < CameraToolbar().yMax + 42)
                return;
            string label = bird.Name + " · " + town.ActivityOf(bird);
            float width = Mathf.Min(map.width - 24, Mathf.Max(140, marker.CalcSize(new GUIContent(label)).x + 24));
            Rect tag = new Rect(Mathf.Clamp(point.x - width / 2, map.x + 8, map.xMax - width - 8), point.y - 36, width, 30);
            Panel(tag, green);
            GUI.Label(tag, label, marker);
            Panel(new Rect(point.x - 2, point.y - 6, 4, 6), green);
        }

        bool GridPoint(Vector2 point, out int x, out int z)
        {
            x = z = 0;
            if (!InTown(point))
                return false;
            var ray = view.ScreenPointToRay(point);
            if (!new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float d))
                return false;
            var p = ray.GetPoint(d);
            x = Mathf.RoundToInt(p.x / 2.2f);
            z = Mathf.RoundToInt(p.z / 2.2f);
            return x >= -town.MapRadius && x <= town.MapRadius && z >= -town.MapRadius && z <= town.MapRadius;
        }

        void ClickTown(Vector2 point)
        {
            if (!GridPoint(point, out int x, out int z))
                return;
            var existing = town.At(x, z);
            if (tool == Tool.Move)
            {
                if (town.Move(selected, x, z))
                {
                    message = "移設しました。鳩の行き先も新しい配置に変わります。";
                    tool = Tool.Inspect;
                }
                else
                    message = "空いているマスを選んでください。";
            }
            else if (existing != null)
            {
                selected = existing.Id;
                tool = Tool.Inspect;
                message = Tips[(int)existing.Kind];
            }
            else if (tool == Tool.Build)
            {
                if (town.Build(building, x, z))
                {
                    selected = town.At(x, z).Id;
                    message = Tips[(int)building];
                }
                else
                    message = town.Notice;
            }
            else
            {
                selected = -1;
                message = "左の施設を選んで、空き地をクリックすると建設できます。";
            }
        }

        void Update()
        {
            if (editingBird < 0 && Input.GetKeyDown(KeyCode.Space))
                paused = !paused;
            if (editingBird < 0)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    StopFollowing();
                    tool = Tool.Inspect;
                    selected = -1;
                }

                CameraInput();
            }
            else
            {
                pointerInTown = false;
                dragged = false;
            }

            UpdateCamera(UnityEngine.Time.unscaledDeltaTime);
            bool hover = GridPoint(Input.mousePosition, out int hx, out int hz);
            world.Preview(hx, hz, hover && !(pointerInTown && dragged) && tool != Tool.Inspect, town.CanPlace(hx, hz, tool == Tool.Move ? selected : -1) && (tool != Tool.Build || town.Money >= TownSimulation.Cost(building)));
            if (!paused)
                for (int i = 0; i < speed; i++)
                    town.Tick(Mathf.Min(UnityEngine.Time.deltaTime, .1f));
            if (town.Season != displayedSeason)
            {
                displayedSeason = town.Season;
                seasonToastUntil = UnityEngine.Time.realtimeSinceStartup + 5;
                message = town.SeasonName + "になりました。街と鳩の過ごし方を眺めてみましょう。";
            }

            world.Sync(town, paused);
            UpdateDaylight();
            if (town.Postcards.Count > postcardCount)
            {
                postcardCount = town.Postcards.Count;
                postcardPage = 0;
                message = town.Notice;
                tab = 3;
                scroll = new Vector2(0, 420);
                Save(false);
            }

            if (town.SeasonalPostcards.Count > seasonalPostcardCount)
            {
                seasonalPostcardCount = town.SeasonalPostcards.Count;
                seasonalPostcardPage = 0;
                message = town.Notice;
                tab = 3;
                scroll = Vector2.zero;
                Save(false);
            }

            saveClock += UnityEngine.Time.unscaledDeltaTime;
            if (saveClock >= 30)
            {
                Save(false);
                saveClock = 0;
            }
        }

        void Save(bool notify)
        {
            if (benchmark)
                return;
            try
            {
                new TownSaveStore(SavePath).Write(JsonUtility.ToJson(town.Capture(), true));
                if (notify)
                    message = "街を保存しました。次回もここから再開します。";
            }
            catch (Exception e)
            {
                message = "保存できませんでした：" + e.Message;
            }
        }

        // Unreadable or newer-version saves are set aside by TownSaveStore, never overwritten by the next autosave.
        void Load()
        {
            int rejected = 0;
            string json = new TownSaveStore(SavePath).Read(text =>
            {
                try
                {
                    if (new TownSimulation().Restore(JsonUtility.FromJson<TownSave>(text)))
                        return true;
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e.Message);
                }

                rejected++;
                return false;
            }

            );
            if (json != null && town.Restore(JsonUtility.FromJson<TownSave>(json)))
                message = rejected > 0 ? "最新の保存を読み込めなかったため、ひとつ前の保存から再開しました。" : "おかえりなさい、市長。保存した街を開きました。";
            else if (rejected > 0)
                message = "保存を読み込めなかったため、新しい広場から開始します。元のファイルは残してあります。";
        }

        // iOS rarely calls OnApplicationQuit; backgrounding is the reliable moment to save.
        void OnApplicationPause(bool pausing)
        {
            if (pausing && town != null)
            {
                CancelRename();
                Save(false);
            }
        }

        void OnApplicationQuit()
        {
            CancelRename();
            if (town != null)
                Save(false);
        }

        void Styles()
        {
            if (text != null)
                return;
            text = new GUIStyle(GUI.skin.label)
            {font = font, fontSize = 17, wordWrap = true, richText = false};
            text.normal.textColor = ink;
            small = new GUIStyle(text)
            {fontSize = 15, fontStyle = FontStyle.Normal};
            small.normal.textColor = muted;
            nameInput = new GUIStyle(GUI.skin.textField)
            {font = font, fontSize = 17, richText = false, padding = new RectOffset(8, 8, 8, 8)};
            marker = new GUIStyle(small)
            {alignment = TextAnchor.MiddleCenter, wordWrap = false};
            marker.normal.textColor = Color.white;
            heading = new GUIStyle(text)
            {fontSize = 22, fontStyle = FontStyle.Bold};
            heading.normal.textColor = ink;
            title = new GUIStyle(heading)
            {fontSize = 30};
            button = new GUIStyle(text)
            {alignment = TextAnchor.MiddleCenter, fontSize = 15, fontStyle = FontStyle.Bold, wordWrap = true, padding = new RectOffset(8, 8, 6, 6)};
        }

        void Panel(Rect r, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        void Label(float x, float y, float w, float h, string value, GUIStyle style = null)
        {
            style = style ?? text;
            GUI.Label(new Rect(x, y, w, Mathf.Max(h, TextHeight(value, w, style))), value, style);
        }

        float TextHeight(string value, float width, GUIStyle style) => Mathf.Ceil(style.CalcHeight(new GUIContent(value), width)) + 6;
        float FlowLabel(float x, float y, float width, string value, GUIStyle style, bool draw = true)
        {
            float height = TextHeight(value, width, style);
            if (draw)
                Label(x, y, width, height, value, style);
            return y + height + 8;
        }

        bool Button(Rect r, string value, bool active = false, bool enabled = true)
        {
            Panel(r, active ? green : enabled ? new Color(.89f, .89f, .82f) : new Color(.92f, .92f, .87f));
            button.normal.textColor = active ? Color.white : enabled ? ink : new Color(.6f, .62f, .56f);
            bool hit = GUI.Button(r, value, button);
            button.normal.textColor = ink;
            return hit && enabled;
        }

        bool RenameButton(Rect r, TownBird bird)
        {
            bool active = editingBird == bird.Id;
            Panel(r, green);
            Panel(new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2), active ? green : paper);
            button.normal.textColor = active ? Color.white : green;
            bool hit = GUI.Button(r, new GUIContent("改名", "「" + bird.Name + "」の名前を変更"), button);
            button.normal.textColor = ink;
            return hit && !active;
        }

        float Meter(float x, float y, float w, string name, float value)
        {
            float barY = FlowLabel(x, y, w, name + "  " + Mathf.RoundToInt(value), small);
            Panel(new Rect(x, barY, w, 5), new Color(.84f, .85f, .77f));
            Panel(new Rect(x, barY, w * Mathf.Clamp01(value / 100), 5), green);
            return barY + 13;
        }

        void OnGUI()
        {
            if (town == null || hideUi)
                return;
            Styles();
            float w = Width, h = Height;
            var l = Layout(w, h);
            GUI.matrix = Matrix4x4.Scale(new Vector3(Scale, Scale, 1));
            Panel(new Rect(0, 0, w, l.headerH), paper);
            Panel(l.left, paper);
            Panel(l.right, paper);
            Panel(l.bottom, paper);
            Panel(new Rect(l.left.width, l.headerH, l.center.x - l.left.width, h - l.headerH), paper);
            Panel(new Rect(l.center.x + l.center.width, l.headerH, w - (l.center.x + l.center.width), h - l.headerH), paper);
            Panel(new Rect(l.center.x, l.headerH, l.center.width, l.center.y - l.headerH), paper);
            Panel(new Rect(l.center.x, l.center.y + l.center.height, l.center.width, l.bottom.y - (l.center.y + l.center.height)), paper);
            float titleBottom = FlowLabel(24, 10, 310, "鳩市長の街づくり", title);
            FlowLabel(24, titleBottom, 310, "鳩と人が暮らす駅前広場", small);
            float moneyBottom = FlowLabel(355, 14, 245, "街の予算  ¥" + town.Money.ToString("0"), heading);
            FlowLabel(355, moneyBottom, 245, "今期収入 ¥" + town.Income.ToString("0") + "  維持費 ¥" + town.Upkeep.ToString("0") + " / 20秒", small);
            Meter(615, 23, 140, "鳩の幸福", town.PigeonHappiness);
            Meter(785, 23, 140, "人の満足", town.HumanSatisfaction);
            float residentX = Mathf.Min(965, w - 470);
            float residentBottom = FlowLabel(residentX, 18, 210, "住民 " + town.Birds.Count + "羽 / 来訪 " + town.Visitors.Count + "人", text);
            FlowLabel(residentX, residentBottom, 210, "" + town.Year + "年目 " + town.SeasonName + town.SeasonDay + "日 · DAY " + town.Day + " · " + town.TimeOfDayName, small);
            float speedW = 104, pauseW = 94, controlGap = 10, speedX = w - 24 - speedW, pauseX = speedX - controlGap - pauseW;
            if (Button(new Rect(pauseX, 25, pauseW, 45), paused ? "再開" : "一時停止", paused))
                paused = !paused;
            // Speed is remembered separately so changing it never resumes a paused town.
            if (Button(new Rect(speedX, 25, speedW, 45), "速度 ×" + speed))
                speed = speed >= 3 ? 1 : 3;
            LeftPanel(l);
            RightPanel(l);
            BottomPanel(l);
            FocusMarker();
            CameraControls();
            if (seasonToastUntil > UnityEngine.Time.realtimeSinceStartup)
            {
                float toastW = 290, toastX = l.center.x + (l.center.width - toastW) / 2;
                Panel(new Rect(toastX, l.center.y + 14, toastW, 52), paper);
                FlowLabel(toastX + 12, l.center.y + 23, toastW - 24, town.SeasonName + "がやってきました", heading);
            }
        }

        void LeftPanel(UiLayout l)
        {
            Rect r = l.left;
            float x = r.x + 16, w = r.width - 32;
            float introY = FlowLabel(x, r.y + 18, w, "街をつくる", heading);
            float introBottom = FlowLabel(x, introY, w, "施設を選んで、中央の街に配置", small);
            string[] uses = {"食料", "水浴び", "寝床", "休憩", "交流", "観光", "商業・交流", "緑地・休息"};
            float gridY = introBottom + 12, gap = 8, bw = (w - 20 - gap) / 2, bh = 58;
            int count = Enum.GetValues(typeof(FacilityKind)).Length;
            for (int i = 0; i < count; i++)
            {
                string caption = TownSimulation.NameOf((FacilityKind)i) + "\n¥" + TownSimulation.Cost((FacilityKind)i) + "\n" + uses[i];
                bh = Mathf.Max(bh, TextHeight(caption, bw, button));
            }

            float inspectY = r.yMax - 328;
            buildScroll = GUI.BeginScrollView(new Rect(x, gridY, w, inspectY - gridY - 12), buildScroll, new Rect(0, 0, w - 20, ((count + 1) / 2) * (bh + gap)), false, true);
            for (int i = 0; i < count; i++)
            {
                var kind = (FacilityKind)i;
                int col = i % 2, row = i / 2;
                if (Button(new Rect(col * (bw + gap), row * (bh + gap), bw, bh), TownSimulation.NameOf(kind) + "\n¥" + TownSimulation.Cost(kind) + "\n" + uses[i], tool == Tool.Build && building == kind))
                    SelectBuilding(kind);
            }

            GUI.EndScrollView();
            if (Button(new Rect(x, inspectY, w, 40), "観察 / 施設を選ぶ", tool == Tool.Inspect))
                tool = Tool.Inspect;
            FlowLabel(x, inspectY + 52, w, "左クリック：建設・選択\n左ドラッグ：マップ移動\n右ドラッグ：視点回転\nホイール / ＋−：ズーム", small);
            float expansionY = r.yMax - 166;
            FlowLabel(x, expansionY, w, "街の広さ " + town.MapSize + " × " + town.MapSize + "マス", small);
            string expansion = town.CanExpand ? "土地を広げる ¥" + town.ExpansionCost + "\n＋" + ((town.MapSize + 2) * (town.MapSize + 2) - town.MapSize * town.MapSize) + "マス（外周1マス）" : "最大まで拡張しました";
            if (Button(new Rect(x, r.yMax - 130, w, 62), expansion, false, town.CanExpand && town.Money >= town.ExpansionCost) && town.ExpandTown())
            {
                CancelRename();
                ResetCamera();
                world.Sync(town, paused);
                message = town.Notice;
                Save(false);
            }

            if (Button(new Rect(x, r.y + r.height - 58, w, 40), "街を保存"))
                Save(true);
        }

        void RightPanel(UiLayout l)
        {
            Rect r = l.right;
            float x = r.x + 16, innerW = r.width - 32;
            float meterGap = 10, meterW = (innerW - meterGap) / 2;
            float foodBottom = Meter(x, r.y + 18, meterW, "食料供給", town.FoodSupply);
            float cleanBottom = Meter(x + meterW + meterGap, r.y + 18, meterW, "清潔さ", town.Cleanliness);
            float tabsTop = FlowLabel(x, Mathf.Max(foodBottom, cleanBottom) + 8, innerW, "混雑 " + town.Crowding.ToString("0") + " · 木と広場でゆとりを", small);
            string[] tabs = {"お願い", "鳩図鑑", "条例", town.FestivalDue && !town.FestivalActive ? "催し！" : "催し"};
            float tabGap = 6, tabW = (innerW - tabGap) / 2, tabY = tabsTop + 8;
            for (int i = 0; i < tabs.Length; i++)
                if (Button(new Rect(x + (i % 2) * (tabW + tabGap), tabY + (i / 2) * 44, tabW, 38), tabs[i], tab == i))
                {
                    CancelRename();
                    tab = i;
                    scroll = Vector2.zero;
                }

            float scrollY = tabY + 94, scrollH = r.y + r.height - scrollY - 16;
            float contentW = innerW - GUI.skin.verticalScrollbar.fixedWidth - 12;
            float content = RightContent(contentW, false);
            scroll = GUI.BeginScrollView(new Rect(x, scrollY, innerW, scrollH), scroll, new Rect(0, 0, contentW, content), false, true);
            RightContent(contentW, true);
            GUI.EndScrollView();
        }

        float RightContent(float width, bool draw)
        {
            float y = 0;
            if (tab == 0)
            {
                y = FlowLabel(0, y, width, "鳩からの小さなお願い", heading, draw);
                y = FlowLabel(0, y, width, "期限はありません。気が向いたら叶えてね。", small, draw) + 8;
                foreach (var wish in town.Wishes)
                {
                    var owner = town.WishOwner(wish);
                    if (owner == null)
                        continue;
                    string caption = owner.Name + "から";
                    float height = Mathf.Max(44, TextHeight(caption, width, button));
                    if (draw && Button(new Rect(0, y, width, height), caption, followedBird == owner.Id))
                        FocusBird(owner.Id);
                    y += height + 8;
                    y = FlowLabel(0, y, width, (wish.Complete ? "✓ " : "") + TownSimulation.WishTitle(wish.Kind), text, draw);
                    y = FlowLabel(0, y, width, TownSimulation.WishDescription(wish.Kind), small, draw);
                    y = FlowLabel(0, y, width, wish.Complete ? "叶いました · ありがとう！" : town.WishHabitatReady(wish) ? "場所は準備できました。使ってくれるのを待とう。" : "街を整えて、居場所をつくろう。", small, draw) + 16;
                }

                y = FlowLabel(0, y, width, "街のみんなからのおたより", heading, draw) + 12;
                for (int i = 0; i < town.Requests.Count; i++)
                {
                    var request = town.Requests[i];
                    y = FlowLabel(0, y, width, (request.Complete ? "✓ " : "0" + (i + 1) + "  ") + request.Title, heading, draw);
                    y = FlowLabel(0, y, width, request.Description, small, draw);
                    y = FlowLabel(0, y, width, request.Complete ? "達成済み · お礼を受け取りました" : "お礼 ¥" + request.Reward, small, draw) + 20;
                }

                y = FlowLabel(0, y, width, "鳩を追い払う必要はありません。居場所と人の通路を、配置で整えましょう。", small, draw);
            }
            else if (tab == 1)
            {
                y = FlowLabel(0, y, width, "この街の住民たち", heading, draw) + 12;
                foreach (var b in town.Birds)
                {
                    string name = (b.Mayor ? "市長 " : b.Rare ? "白い羽 " : "") + b.Name;
                    const float editWidth = 60, headerGap = 8;
                    float nameWidth = width - editWidth - headerGap;
                    float nameHeight = Mathf.Max(44, TextHeight(name, nameWidth, button));
                    if (draw && Button(new Rect(0, y, nameWidth, nameHeight), name, followedBird == b.Id))
                        FocusBird(b.Id);
                    if (draw && RenameButton(new Rect(nameWidth + headerGap, y + (nameHeight - 44) / 2, editWidth, 44), b))
                        BeginRename(b);
                    y += nameHeight + 8;
                    y = FlowLabel(0, y, width, TownSimulation.FeatherName(TownSimulation.FeatherOf(b)) + " · " + TownSimulation.PersonalityName(b.Personality) + " · " + town.ActivityOf(b), small, draw);
                    var friend = town.BestFriendOf(b);
                    if (friend != null)
                        y = FlowLabel(0, y, width, "よく一緒にいる：" + friend.Name, small, draw);
                    y = RenameControls(b, y, width, draw);
                    if (draw)
                        Panel(new Rect(0, y + 8, width, 1), new Color(.79f, .81f, .74f));
                    y += 29;
                }

                y = FlowLabel(0, y, width, "羽色の図鑑", heading, draw) + 12;
                for (int i = 0; i < 5; i++)
                {
                    var feather = (Plumage)i;
                    bool found = town.Discovered(feather);
                    if (draw)
                        GUI.DrawTexture(new Rect(0, y, 52, 52), FeatherPortrait(feather, found));
                    y = FlowLabel(62, y, width - 62, found ? TownSimulation.FeatherName(feather) : "未発見 · ？", button, draw);
                    y += 28;
                    if (found)
                    {
                        string names = string.Join("、", town.Birds.FindAll(b => TownSimulation.FeatherOf(b) == feather).ConvertAll(b => b.Name).ToArray());
                        y = FlowLabel(0, y, width, "出会った仲間：" + names, small, draw);
                    }

                    y = FlowLabel(0, y, width, town.FeatherHint(feather), small, draw) + 22;
                }
            }
            else if (tab == 2)
            {
                y = FlowLabel(0, y, width, "市長のひと声", heading, draw) + 12;
                string[] names = {"公共施設に巣箱", "水浴び優先区域", "オープンカフェ支援"};
                string[] notes = {"寝床が増え、鳩が安心。維持費が増えます。", "水浴びが充実。人間のイベント空間は少し減少。", "食料供給と商業を支援。清掃需要と維持費が増加。"};
                bool[] on = {town.NestBoxes, town.BathPriority, town.CafeSupport};
                for (int i = 0; i < 3; i++)
                {
                    string caption = names[i] + (on[i] ? " ON" : " OFF");
                    float height = Mathf.Max(48, TextHeight(caption, width, button));
                    if (draw && Button(new Rect(0, y, width, height), caption, on[i]))
                        town.TogglePolicy((TownPolicy)i);
                    y += height + 10;
                    y = FlowLabel(0, y, width, notes[i], small, draw) + 24;
                }
            }
            else
                y = FestivalContent(width, draw);
            return y + 16;
        }

        float FestivalContent(float width, bool draw)
        {
            float y = SeasonalContent(width, draw) + 24;
            y = FlowLabel(0, y, width, "街の催し", heading, draw) + 4;
            y = FlowLabel(0, y, width, "3日ごとに開けます。施設の配置と、鳩や人の実際の行動で絵はがきが届きます。", small, draw) + 8;
            string calendar = town.FestivalActive ? "ただいま開催中" : town.FestivalDue ? "開催できます！" : "次の開催は DAY " + town.NextFestivalDay;
            y = FlowLabel(0, y, width, calendar, text, draw) + 9;
            var selected = town.SelectedFestival;
            y = FlowLabel(0, y, width, "会場：" + TownSimulation.FestivalVenueHint(selected), small, draw);
            bool venueReady = town.FestivalActive ? town.FestivalCurrentVenueReady : town.FestivalVenueReady(selected);
            y = FlowLabel(0, y, width, venueReady ? "✓ 会場ができています" : town.FestivalActive ? "会場を整えると観察を再開できます" : "会場を整えると開催できます", small, draw) + 7;
            if (!town.FestivalActive)
            {
                if (draw && Button(new Rect(0, y, width, 50), town.FestivalDue ? "催しを開く" : "DAY " + town.NextFestivalDay + "まで待つ", false, town.FestivalDue && venueReady) && town.StartFestival())
                {
                    message = town.Notice;
                    Save(false);
                }

                y += 65;
                y = FlowLabel(0, y, width, "催しを選ぶ", heading, draw) + 4;
                for (int i = 0; i < 3; i++)
                {
                    var kind = (FestivalKind)i;
                    if (draw && Button(new Rect(0, y, width, 48), TownSimulation.FestivalName(kind), town.SelectedFestival == kind))
                    {
                        town.ChooseFestival(kind);
                        Save(false);
                    }

                    y += 56;
                }
            }
            else
            {
                y = FlowLabel(0, y, width, "街を眺めて、次の場面を見つけよう。", small, draw) + 6;
                bool main = town.FestivalMainBirdIds.Count > 0, partner = town.FestivalPartnerBirdIds.Count > 0;
                if (selected == FestivalKind.BakeryMarket)
                {
                    y = FlowLabel(0, y, width, (main ? "✓ " : "○ ") + "鳩がパン屋で食事", text, draw);
                    y = FlowLabel(0, y, width, (town.FestivalHumanPurchase ? "✓ " : "○ ") + "人がパンを購入", text, draw);
                }
                else if (selected == FestivalKind.WatersideDay)
                {
                    y = FlowLabel(0, y, width, (main ? "✓ " : "○ ") + "鳩が噴水で水浴び", text, draw);
                    y = FlowLabel(0, y, width, (partner ? "✓ " : "○ ") + "鳩が近くの公園で休憩", text, draw);
                }
                else
                    y = FlowLabel(0, y, width, (main ? "✓ " : "○ ") + "夕方、鳩が時計台に止まる", text, draw);
                y = FlowLabel(0, y, width, "期限も失敗のペナルティもありません。", small, draw) + 8;
                if (draw && Button(new Rect(0, y, width, 44), "準備に戻る") && town.CancelFestival())
                {
                    message = town.Notice;
                    Save(false);
                }

                y += 59;
            }

            int[] totals = new int[3];
            foreach (var card in town.Postcards)
                totals[(int)card.Kind]++;
            y = FlowLabel(0, y, width, "絵はがき  " + town.Postcards.Count + "枚", heading, draw);
            y = FlowLabel(0, y, width, "朝市 " + totals[0] + "  ·  水辺 " + totals[1] + "  ·  時計台 " + totals[2], small, draw) + 9;
            if (town.Postcards.Count == 0)
                y = FlowLabel(0, y, width, "最初の一枚には、参加した鳩の名前が残ります。", small, draw);
            const int cardsPerPage = 8;
            int lastPage = Mathf.Max(0, (town.Postcards.Count - 1) / cardsPerPage);
            int page = Mathf.Min(postcardPage, lastPage);
            if (town.Postcards.Count > cardsPerPage)
                y = FlowLabel(0, y, width, (page + 1) + " / " + (lastPage + 1) + "ページ", small, draw) + 4;
            for (int i = town.Postcards.Count - 1 - page * cardsPerPage; i >= 0 && i > town.Postcards.Count - 1 - (page + 1) * cardsPerPage; i--)
            {
                var card = town.Postcards[i];
                float start = y;
                float cy = y + 12;
                float captionW = width - 94;
                cy = FlowLabel(82, cy, captionW, "POSTCARD / DAY " + card.Day, small, false);
                cy = FlowLabel(82, cy, captionW, TownSimulation.FestivalName(card.Kind), heading, false);
                cy = Mathf.Max(cy, start + 78);
                cy = FlowLabel(13, cy, width - 26, "参加：" + string.Join("、", card.BirdNames.ToArray()), small, false);
                if (draw)
                {
                    Panel(new Rect(0, start, width, cy - start + 8), new Color(.95f, .9f, .79f));
                    Panel(new Rect(0, start, 5, cy - start + 8), green);
                    GUI.DrawTexture(new Rect(12, start + 12, 60, 60), FestivalPortrait(card.Kind));
                    float py = start + 12;
                    py = FlowLabel(82, py, captionW, "POSTCARD / DAY " + card.Day, small);
                    py = FlowLabel(82, py, captionW, TownSimulation.FestivalName(card.Kind), heading);
                    FlowLabel(13, Mathf.Max(py, start + 78), width - 26, "参加：" + string.Join("、", card.BirdNames.ToArray()), small);
                }

                y = cy + 19;
            }

            if (town.Postcards.Count > cardsPerPage)
            {
                float half = (width - 8) / 2;
                if (draw && Button(new Rect(0, y, half, 42), "新しい方へ", false, page > 0))
                {
                    postcardPage = page - 1;
                    scroll = Vector2.zero;
                }

                if (draw && Button(new Rect(half + 8, y, half, 42), "古い方へ", false, page < lastPage))
                {
                    postcardPage = page + 1;
                    scroll = Vector2.zero;
                }

                y += 56;
            }

            return y;
        }

        float SeasonalContent(float width, bool draw)
        {
            var season = town.Season;
            float y = FlowLabel(0, 0, width, "季節の催し · " + town.SeasonName, heading, draw) + 4;
            string seasonClock = town.SeasonDaysRemaining == 0 ? "今季の最終日" : "あと" + town.SeasonDaysRemaining + "日";
            y = FlowLabel(0, y, width, town.Year + "年目 / " + seasonClock + "。逃しても来年また楽しめます。", small, draw) + 8;
            y = FlowLabel(0, y, width, TownSimulation.SeasonalFestivalName(season), heading, draw) + 3;
            y = FlowLabel(0, y, width, "会場：" + TownSimulation.SeasonalVenueHint(season), small, draw);
            y = FlowLabel(0, y, width, town.SeasonalVenueReady ? "✓ 会場が整っています" : "○ 会場を整えると観察できます", small, draw) + 7;
            string action = season == TownSeason.Spring ? "鳩が公園で羽繕いか日向ぼっこ" : season == TownSeason.Summer ? "鳩が噴水で水浴び" : season == TownSeason.Autumn ? "鳩がパン屋で食事" : "鳩が木で休憩";
            y = FlowLabel(0, y, width, (town.SeasonalBirdObserved ? "✓ " : "○ ") + action, text, draw);
            if (season == TownSeason.Autumn)
                y = FlowLabel(0, y, width, (town.SeasonalHumanPurchase ? "✓ " : "○ ") + "人がパン屋で買い物", text, draw);
            y = FlowLabel(0, y, width, town.SeasonalEventCompleted ? "今季の絵はがきが届きました！" : "鳩たちを眺めて完成を待ちましょう。", small, draw) + 14;
            y = FlowLabel(0, y, width, "季節の絵はがき  " + town.SeasonalPostcards.Count + "枚", heading, draw) + 7;
            const int cardsPerPage = 4;
            int lastPage = Mathf.Max(0, (town.SeasonalPostcards.Count - 1) / cardsPerPage);
            int page = Mathf.Min(seasonalPostcardPage, lastPage);
            if (town.SeasonalPostcards.Count == 0)
                y = FlowLabel(0, y, width, "最初の一枚には、参加した鳩の名前が残ります。", small, draw) + 4;
            for (int i = town.SeasonalPostcards.Count - 1 - page * cardsPerPage; i >= 0 && i > town.SeasonalPostcards.Count - 1 - (page + 1) * cardsPerPage; i--)
            {
                var card = town.SeasonalPostcards[i];
                float start = y, captionW = width - 94;
                string cardTitle = card.Year + "年目 · " + TownSimulation.SeasonalFestivalName(card.Season);
                float cy = FlowLabel(82, y + 12, captionW, cardTitle, text, false);
                cy = FlowLabel(82, cy, captionW, "DAY " + card.Day, small, false);
                cy = FlowLabel(13, Mathf.Max(cy, start + 78), width - 26, "参加：" + string.Join("、", card.BirdNames.ToArray()), small, false);
                if (draw)
                {
                    Panel(new Rect(0, start, width, cy - start + 8), new Color(.91f, .93f, .86f));
                    Panel(new Rect(0, start, 5, cy - start + 8), green);
                    GUI.DrawTexture(new Rect(12, start + 12, 60, 60), SeasonalPortrait(card.Season));
                    float py = FlowLabel(82, start + 12, captionW, cardTitle, text);
                    py = FlowLabel(82, py, captionW, "DAY " + card.Day, small);
                    FlowLabel(13, Mathf.Max(py, start + 78), width - 26, "参加：" + string.Join("、", card.BirdNames.ToArray()), small);
                }

                y = cy + 19;
            }

            if (town.SeasonalPostcards.Count > cardsPerPage)
            {
                float half = (width - 8) / 2;
                if (draw && Button(new Rect(0, y, half, 42), "新しい方へ", false, page > 0))
                {
                    seasonalPostcardPage = page - 1;
                    scroll = Vector2.zero;
                }

                if (draw && Button(new Rect(half + 8, y, half, 42), "古い方へ", false, page < lastPage))
                {
                    seasonalPostcardPage = page + 1;
                    scroll = Vector2.zero;
                }

                y += 56;
            }

            return y;
        }

        readonly Texture2D[] seasonalPortraits = new Texture2D[4];
        Texture2D SeasonalPortrait(TownSeason season)
        {
            int index = (int)season;
            if (seasonalPortraits[index] != null)
                return seasonalPortraits[index];
            var texture = new Texture2D(64, 64);
            texture.filterMode = FilterMode.Point;
            var pixels = new Color[64 * 64];
            Color sky = new[]{new Color(.94f, .82f, .83f), new Color(.65f, .84f, .84f), new Color(.91f, .72f, .52f), new Color(.79f, .86f, .89f)}[index];
            Color ground = new[]{new Color(.75f, .83f, .65f), new Color(.55f, .76f, .61f), new Color(.73f, .66f, .47f), new Color(.82f, .84f, .77f)}[index];
            Color accent = new[]{new Color(.88f, .48f, .58f), new Color(.27f, .63f, .7f), new Color(.7f, .38f, .2f), new Color(.5f, .55f, .56f)}[index];
            Color bird = new Color(.33f, .4f, .44f);
            for (int py = 0; py < 64; py++)
                for (int px = 0; px < 64; px++)
                {
                    Color color = py < 18 ? ground : sky;
                    int dx = px - 39, dy = py - 25;
                    if (dx * dx / 140f + dy * dy / 68f < 1 || (px - 47) * (px - 47) + (py - 34) * (py - 34) < 37)
                        color = bird;
                    if (px >= 51 && px <= 57 && py >= 33 && py <= 36)
                        color = new Color(.86f, .7f, .47f);
                    if (index == 0 && (px - 17) * (px - 17) + (py - 43) * (py - 43) < 135)
                        color = accent;
                    if (index == 1 && (px - 18) * (px - 18) / 200f + (py - 21) * (py - 21) / 30f < 1)
                        color = accent;
                    if (index == 2 && px > 10 && px < 25 && py > 38 && py < 49 && py < 49 - Math.Abs(px - 17) * .55f)
                        color = accent;
                    if (index == 3 && px >= 15 && px <= 18 && py >= 18 && py <= 46)
                        color = accent;
                    if (index == 3 && py >= 38 && py <= 41 && px >= 8 && px <= 28)
                        color = accent;
                    pixels[py * 64 + px] = color;
                }

            texture.SetPixels(pixels);
            texture.Apply();
            seasonalPortraits[index] = texture;
            return texture;
        }

        readonly Texture2D[] festivalPortraits = new Texture2D[3];
        Texture2D FestivalPortrait(FestivalKind kind)
        {
            int index = (int)kind;
            if (festivalPortraits[index] != null)
                return festivalPortraits[index];
            var texture = new Texture2D(64, 64);
            texture.filterMode = FilterMode.Point;
            var pixels = new Color[64 * 64];
            Color sky = index == 0 ? new Color(.98f, .81f, .59f) : index == 1 ? new Color(.69f, .87f, .84f) : new Color(.78f, .72f, .83f);
            Color ground = new Color(.87f, .87f, .7f), stone = new Color(.91f, .88f, .76f), dark = new Color(.24f, .37f, .34f), water = new Color(.36f, .7f, .72f);
            for (int py = 0; py < 64; py++)
                for (int px = 0; px < 64; px++)
                {
                    Color color = py < 14 ? ground : sky;
                    if (index == 0)
                    {
                        if (px >= 13 && px < 52 && py >= 13 && py < 35)
                            color = stone;
                        if (px >= 9 && px < 56 && py >= 33 && py < 45 && py < 45 - Math.Abs(px - 32) * .42f)
                            color = new Color(.72f, .36f, .26f);
                        if (px >= 20 && px < 44 && py >= 21 && py < 27)
                            color = new Color(.99f, .97f, .84f);
                        if (px >= 27 && px < 37 && py >= 13 && py < 21)
                            color = dark;
                        if ((px - 32) * (px - 32) / 150f + (py - 50) * (py - 50) / 18f < 1)
                            color = new Color(.98f, .85f, .42f);
                    }
                    else if (index == 1)
                    {
                        if ((px - 32) * (px - 32) / 550f + (py - 21) * (py - 21) / 140f < 1)
                            color = stone;
                        if ((px - 32) * (px - 32) / 400f + (py - 23) * (py - 23) / 65f < 1)
                            color = water;
                        if (px >= 28 && px < 36 && py >= 25 && py < 42)
                            color = stone;
                        if ((px - 32) * (px - 32) / 60f + (py - 43) * (py - 43) / 20f < 1)
                            color = water;
                        if ((px == 18 || px == 45) && py >= 36 && py < 43)
                            color = water;
                    }
                    else
                    {
                        if (px >= 20 && px < 44 && py >= 12 && py < 48)
                            color = stone;
                        if (px >= 17 && px < 47 && py >= 47 && py < 53)
                            color = dark;
                        if (px >= 29 && px < 35 && py >= 12 && py < 27)
                            color = dark;
                        int dx = px - 32, dy = py - 39;
                        if (dx * dx + dy * dy < 72)
                            color = new Color(.98f, .95f, .81f);
                        if (dx * dx + dy * dy < 72 && (Math.Abs(dx) <= 1 && dy >= 0 || Math.Abs(dy) <= 1 && dx >= 0))
                            color = dark;
                    }

                    pixels[py * 64 + px] = color;
                }

            texture.SetPixels(pixels);
            texture.Apply();
            festivalPortraits[index] = texture;
            return texture;
        }

        readonly Texture2D[] featherPortraits = new Texture2D[6];
        Texture2D FeatherPortrait(Plumage feather, bool found)
        {
            int index = found ? (int)feather : 5;
            if (featherPortraits[index] != null)
                return featherPortraits[index];
            var texture = new Texture2D(64, 64);
            var pixels = new Color[64 * 64];
            Color color;
            ColorUtility.TryParseHtmlString(new[]{"#7D8B96", "#586470", "#9C755E", "#EEEDE3", "#EFE9D8", "#34483E"}[index], out color);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float body = (x - 29) * (x - 29) / 324f + (y - 25) * (y - 25) / 225f;
                    float head = (x - 43) * (x - 43) / 81f + (y - 45) * (y - 45) / 81f;
                    bool neck = x > 34 && x < 49 && y > 24 && y < 46, tail = x > 5 && x < 25 && y > 14 && y < 24, beak = x >= 49 && x < 60 && y > 40 && y < 45;
                    if (body < 1 || head < 1 || neck || tail || beak)
                        pixels[y * 64 + x] = color;
                    if (found && body < .6f && x < 35)
                        pixels[y * 64 + x] = feather == Plumage.Pied ? new Color(.19f, .23f, .28f) : color * .8f;
                }

            texture.SetPixels(pixels);
            texture.Apply();
            featherPortraits[index] = texture;
            return texture;
        }

        void BottomPanel(UiLayout l)
        {
            Rect r = l.bottom;
            float x = r.x + 18, width = r.width - 36;
            var f = town.Facilities.Find(a => a.Id == selected);
            if (f != null && tool != Tool.Build)
            {
                float y = FlowLabel(x, r.y + 10, width, TownSimulation.NameOf(f.Kind) + " Lv." + f.Level + " / " + f.X + ", " + f.Z, heading);
                FlowLabel(x, y, width, tool == Tool.Move ? "空き地をクリックして移設。費用はかかりません。" : Tips[(int)f.Kind], small);
                float by = r.yMax - 58;
                if (Button(new Rect(x, by, 110, 44), "移設 ¥0", tool == Tool.Move))
                    tool = Tool.Move;
                int cost = TownSimulation.Cost(f.Kind) * f.Level / 2;
                if (Button(new Rect(x + 120, by, 132, 44), f.Level >= 3 ? "改良済み" : "改良 ¥" + cost, false, f.Level < 3 && town.Money >= cost))
                    town.Upgrade(f.Id);
                if (Button(new Rect(x + width - 150, by, 150, 44), "撤去 / 70%返金"))
                {
                    town.Remove(f.Id);
                    selected = -1;
                    tool = Tool.Inspect;
                }
            }
            else
            {
                float y = FlowLabel(x, r.y + 10, width, tool == Tool.Build ? TownSimulation.NameOf(building) + "を建てる" : "市長の観察ノート", heading);
                y = FlowLabel(x, y, width, message, small);
                FlowLabel(x, y, width, town.Notice, small);
            }
        }
    }
}
