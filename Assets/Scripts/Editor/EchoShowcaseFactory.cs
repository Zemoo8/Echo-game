using System.Collections.Generic;
using Echo.Audio;
using Echo.Core;
using Echo.Presentation;
using Echo.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static Echo.EditorTools.EchoUiFactory;

namespace Echo.EditorTools
{
    /// <summary>
    /// Builds the presentation phase 3 scene (Showcase_Design): an in-engine slide deck about
    /// ECHO's look. The world holds six dressed sections side by side (the hero's animations,
    /// the threats, and one set per biome); the canvas holds the titles, captions and the
    /// palette, label and interface overlays. Code-generated, never hand-edited.
    /// </summary>
    public static class EchoShowcaseFactory
    {
        public static string ScenePath => $"{EchoSceneFactory.ScenesFolder}/Presentation/{SceneNames.DesignShowcase}.unity";

        const float CameraY = 2.6f;
        const float HalfWidth = 12f;
        // 640x360 world pixels at 32 px per unit on a 960x540 canvas: 48 canvas units per world unit.
        const float CanvasPerUnit = 48f;

        const float HeroX = 0f;
        const float ThreatsX = 30f;
        const float LabX = 60f;
        const float SecurityX = 90f;
        const float CoreX = 120f;
        const float ReactorX = 150f;

        static readonly Color CaptionColor = new Color(0.85f, 0.92f, 0.96f);

        [MenuItem("ECHO/Setup/Rebuild Design Showcase")]
        public static void Rebuild()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EchoSceneFactory.EnsureScene(ScenePath, Build, true);
            EchoProjectSetup.SetupBuildScenes();
        }

        public static void Ensure() => EchoSceneFactory.EnsureScene(ScenePath, Build, false);

        static void Build()
        {
            var lab = EchoArtFactory.LoadKit(Biome.Lab);
            var security = EchoArtFactory.LoadKit(Biome.Security);
            var core = EchoArtFactory.LoadKit(Biome.Core);
            var reactor = EchoArtFactory.LoadKit(Biome.Reactor);

            var camera = EchoSceneFactory.CreateCamera(lab.voidColor);
            camera.transform.position = new Vector3(LabX, CameraY, -10f);
            EchoLevelArt.DustMotes(camera.transform);
            var globalLight = EchoLevelArt.CreateGlobalLight(lab.ambientIntensity, lab.ambientColor);

            var world = new GameObject("World").transform;
            HeroStudio(Section(world, "HeroStudio"), lab, HeroX);
            ThreatStudio(Section(world, "ThreatStudio"), lab, ThreatsX);
            LabSet(Section(world, "Lab"), lab, LabX);
            SecuritySet(Section(world, "Security"), security, SecurityX);
            CoreSet(Section(world, "Core"), core, CoreX);
            ReactorSet(Section(world, "Reactor"), reactor, ReactorX);

            BuildUI(camera, globalLight, lab, security, core, reactor);
        }

        static Transform Section(Transform world, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(world, false);
            return go.transform;
        }

        // --- world sections ---------------------------------------------------------------------

        static void Ground(Transform parent, EnvironmentKit kit, float xMin, float xMax)
        {
            var go = new GameObject("Ground") { layer = LayerMask.NameToLayer(Layers.Ground) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3((xMin + xMax) * 0.5f, -2.5f, 0f);
            EchoLevelArt.DressBlock(go, new Vector2(xMax - xMin, 5f), kit);
            EchoLevelArt.DepthShade(parent, xMin, xMax, -0.6f);
        }

        // A dim stage for the character and threat line-ups: lab walls pushed into the dark.
        static void Studio(Transform parent, EnvironmentKit lab, float x)
        {
            EchoLevelArt.WallStrip(lab, parent, x - HalfWidth, x + HalfWidth, -0.25f);
            var shade = new GameObject("StudioShade");
            shade.transform.SetParent(parent, false);
            shade.transform.position = new Vector3(x, 3f, 0f);
            var renderer = shade.AddComponent<SpriteRenderer>();
            renderer.sprite = EchoSpriteFactory.Load(EchoSpriteFactory.Square);
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(HalfWidth * 2f, 12f);
            renderer.color = new Color(lab.voidColor.r, lab.voidColor.g, lab.voidColor.b, 0.6f);
            renderer.sortingLayerName = "Background";
            renderer.sortingOrder = 4;
            Ground(parent, lab, x - HalfWidth, x + HalfWidth);
            EchoLevelArt.CeilingLamp(parent, x - 5f, 6f, false, lab.lampColor);
            EchoLevelArt.CeilingLamp(parent, x + 5f, 6f, false, lab.lampColor);
        }

        static readonly float[] HeroSpots = { -7.2f, -3.6f, 0f, 3.6f, 7.2f };

        static void HeroStudio(Transform parent, EnvironmentKit lab, float x)
        {
            Studio(parent, lab, x);
            var visuals = EchoArtFactory.EnsurePlayerVisuals();
            Pose(parent, "Idle", visuals.idle, new Vector2(x + HeroSpots[0], 0f), visuals.idle.framesPerSecond);
            Pose(parent, "Run", visuals.run, new Vector2(x + HeroSpots[1], 0f), visuals.run.framesPerSecond);
            Pose(parent, "Jump", visuals.apexFlip, new Vector2(x + HeroSpots[2], 1.2f), 8f);
            var dash = Pose(parent, "Dash", visuals.dash, new Vector2(x + HeroSpots[3], 0.4f), 1f);
            DashGhosts(dash.transform, visuals.dash.frames[0]);
            Pose(parent, "Victory", visuals.victory, new Vector2(x + HeroSpots[4], 0f), 8f);
        }

        static GameObject Pose(Transform parent, string name, PlayerVisuals.Clip clip, Vector2 feet, float fps)
        {
            var go = new GameObject("Hero_" + name);
            go.transform.SetParent(parent, false);
            go.transform.position = feet;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = clip.frames[0];
            renderer.sortingLayerName = "Player";
            if (clip.frames.Length > 1)
                Flipbook(go, clip.frames, fps);
            return go;
        }

        // Frozen afterimages behind the dash pose, the way the dash effect leaves them.
        static void DashGhosts(Transform hero, Sprite frame)
        {
            for (int i = 1; i <= 3; i++)
            {
                var ghost = new GameObject("Afterimage");
                ghost.transform.SetParent(hero, false);
                ghost.transform.localPosition = new Vector3(-0.55f * i, 0f, 0f);
                var renderer = ghost.AddComponent<SpriteRenderer>();
                renderer.sprite = frame;
                renderer.sharedMaterial = EchoArtFactory.GlowMaterial;
                renderer.color = new Color(0.21f, 0.89f, 1f, 0.55f - 0.15f * i);
                renderer.sortingLayerName = "Player";
                renderer.sortingOrder = -i;
            }
        }

        static readonly float[] ThreatSpots = { -8f, -4.8f, -1.6f, 2.2f, 5.2f, 8.2f };

        static void ThreatStudio(Transform parent, EnvironmentKit lab, float x)
        {
            Studio(parent, lab, x);

            const string enemies = "Assets/ThirdParty/Ansimuz/WarpedCity/Enemies";
            var drone = Animated(parent, "Drone", NumberedFromOne($"{enemies}/drone/drone_{{0:00}}.png", 4), 10f,
                                 new Vector2(x + ThreatSpots[0], 2.4f), "Characters");
            drone.AddComponent<Bob>();
            var turretFrames = NumberedFromOne($"{enemies}/turret/turret_{{0:00}}.png", 6);
            Animated(parent, "Turret", turretFrames, 8f,
                     new Vector2(x + ThreatSpots[1], turretFrames[0].bounds.size.y * 0.5f), "Characters");

            // A pit cut into the floor.
            float pitMin = x + ThreatSpots[2] - 1.5f;
            float pitMax = x + ThreatSpots[2] + 1.5f;
            var floorCut = new GameObject("PitHole");
            floorCut.transform.SetParent(parent, false);
            floorCut.transform.position = new Vector3((pitMin + pitMax) * 0.5f, -2.5f, 0f);
            var hole = floorCut.AddComponent<SpriteRenderer>();
            hole.sprite = EchoSpriteFactory.Load(EchoSpriteFactory.Square);
            hole.drawMode = SpriteDrawMode.Sliced;
            hole.size = new Vector2(pitMax - pitMin, 5.2f);
            hole.color = lab.voidColor;
            hole.sortingLayerName = "Level";
            hole.sortingOrder = 6;
            EchoLevelArt.Pit(parent, pitMin, pitMax, 0f, -3f);

            var cell = new GameObject("EnergyCell");
            cell.transform.SetParent(parent, false);
            cell.transform.position = new Vector3(x + ThreatSpots[3], 1f, 0f);
            EchoLevelArt.DressEnergyCell(cell);

            Barrier(parent, x + ThreatSpots[4], false);
            Barrier(parent, x + ThreatSpots[5], true);
        }

        static void Barrier(Transform parent, float x, bool exit)
        {
            var go = new GameObject(exit ? "ExitDoor" : "Barrier");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, 2.5f, 0f);
            EchoLevelArt.DressBarrier(go, 5f, exit);
        }

        static void LabSet(Transform parent, EnvironmentKit lab, float x)
        {
            const float corridorFloor = -0.25f;
            EchoLevelArt.FarCity(parent, x - HalfWidth - 2f, x + HalfWidth + 2f, corridorFloor, parallax: false);
            EchoLevelArt.CorridorPieces(parent, x - 13.5f, corridorFloor,
                "Corridor_A", "Corridor_Machinery", "Corridor_RoomEnd", "Corridor_Terminal", "Corridor_A");
            EchoLevelArt.UpperMachinery(parent, x - HalfWidth - 2f, x + HalfWidth + 2f, corridorFloor + 5.5f);
            Ground(parent, lab, x - HalfWidth, x + HalfWidth);

            EchoLevelArt.Prop(parent, EchoLevelArt.LabProp("ServerCabinets"), x - 6.5f, 0f);
            EchoLevelArt.Prop(parent, EchoLevelArt.LabProp("CryoPod"), x + 9.4f, 0f);
            var cell = new GameObject("EnergyCell");
            cell.transform.SetParent(parent, false);
            cell.transform.position = new Vector3(x + 1.8f, 1.1f, 0f);
            EchoLevelArt.DressEnergyCell(cell);
            EchoLevelArt.CeilingLamp(parent, x - 6f, 4.2f, false, lab.lampColor);
            EchoLevelArt.CeilingLamp(parent, x + 2f, 4.2f, true, lab.lampColor);
            EchoLevelArt.CeilingLamp(parent, x + 9f, 4.2f, false, lab.lampColor);
            EchoLevelArt.Hero(parent, new Vector2(x + 4.4f, 0f), faceLeft: true);
        }

        static void SecuritySet(Transform parent, EnvironmentKit kit, float x)
        {
            EchoLevelArt.SecurityHall(kit, parent, x - HalfWidth - 1f, x + HalfWidth + 1f, -0.25f);
            Ground(parent, kit, x - HalfWidth, x + HalfWidth);
            EchoLevelArt.Prop(parent, Path(kit.props[0]), x + 6.5f, 0f);  // security door
            EchoLevelArt.Prop(parent, Path(kit.props[1]), x - 8f, 0f);
            EchoLevelArt.Prop(parent, Path(kit.props[2]), x - 6.8f, 0f);
            EchoLevelArt.Prop(parent, Path(kit.props[3]), x + 9.5f, 0f);
            EchoLevelArt.CeilingLamp(parent, x - 3f, 5.6f, false, kit.lampColor);
            EchoLevelArt.CeilingLamp(parent, x + 4f, 5.6f, true, kit.lampColor);

            const string enemies = "Assets/ThirdParty/Ansimuz/WarpedCity/Enemies";
            var turretFrames = NumberedFromOne($"{enemies}/turret/turret_{{0:00}}.png", 6);
            Animated(parent, "Turret", turretFrames, 8f, new Vector2(x + 3f, turretFrames[0].bounds.size.y * 0.5f), "Characters");
            EchoLevelArt.Hero(parent, new Vector2(x - 2.5f, 0f), faceLeft: false);
        }

        static void CoreSet(Transform parent, EnvironmentKit kit, float x)
        {
            EchoLevelArt.CoreMachinery(kit, parent, x - HalfWidth - 1f, x + HalfWidth + 1f, -0.25f);
            Ground(parent, kit, x - HalfWidth, x + HalfWidth);
            EchoLevelArt.Prop(parent, Path(kit.props[0]), x - 6f, 0f);
            EchoLevelArt.Prop(parent, Path(kit.props[1]), x + 5f, 0f);
            EchoLevelArt.Prop(parent, Path(kit.props[2]), x + 8f, 0f);
            foreach (float cellX in new[] { 0.5f, 2.3f })
            {
                var cell = new GameObject("EnergyCell");
                cell.transform.SetParent(parent, false);
                cell.transform.position = new Vector3(x + cellX, 1.2f, 0f);
                EchoLevelArt.DressEnergyCell(cell);
            }
            EchoLevelArt.Hero(parent, new Vector2(x - 2.5f, 0f), faceLeft: false);
        }

        static void ReactorSet(Transform parent, EnvironmentKit kit, float x)
        {
            EchoLevelArt.CoreMachinery(kit, parent, x - HalfWidth - 1f, x + HalfWidth + 1f, -0.25f);
            Ground(parent, kit, x - HalfWidth, x + HalfWidth);
            EchoLevelArt.Reactor(parent, new Vector2(x - 1.5f, 3.6f), overload: false);
            EchoLevelArt.Reactor(parent, new Vector2(x + 4.5f, 3.6f), overload: true);
            EchoLevelArt.Hero(parent, new Vector2(x - 7.5f, 0f), faceLeft: false);
        }

        static GameObject Animated(Transform parent, string name, Sprite[] frames, float fps, Vector2 position, string layer)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.sortingLayerName = layer;
            Flipbook(go, frames, fps);
            return go;
        }

        static void Flipbook(GameObject go, Sprite[] frames, float fps)
        {
            var flipbook = go.AddComponent<SpriteFlipbook>();
            var settings = new SerializedObject(flipbook);
            var array = settings.FindProperty("frames");
            array.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            settings.FindProperty("framesPerSecond").floatValue = fps;
            settings.FindProperty("randomStart").boolValue = false;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        static string Path(Sprite sprite) => AssetDatabase.GetAssetPath(sprite);

        // The Warped City enemy frames are numbered from 01.
        static Sprite[] NumberedFromOne(string format, int count)
        {
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
                frames[i] = EchoArtFactory.LoadSprite(string.Format(format, i + 1));
            return frames;
        }

        // --- interface --------------------------------------------------------------------------

        static void BuildUI(Camera camera, UnityEngine.Rendering.Universal.Light2D globalLight,
                            EnvironmentKit lab, EnvironmentKit security, EnvironmentKit core, EnvironmentKit reactor)
        {
            var plain = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = CreateCanvas("ShowcaseCanvas", null, 0);

            var dimmer = NewImage("Dimmer", canvas, EchoSpriteFactory.Load(EchoSpriteFactory.Square), WithAlpha(lab.voidColor, 0f));
            Stretch(dimmer.rectTransform);

            // Overlays sit between the dimmer and the titles.
            var intro = IntroOverlay(canvas, plain);
            var palette = PaletteOverlay(canvas, plain);
            var heroLabels = Labels(canvas, "HeroLabels", HeroX, HeroX, HeroSpots, -0.7f, "IDLE", "RUN", "JUMP", "DASH", "VICTORY");
            var threatLabels = Labels(canvas, "ThreatLabels", ThreatsX, ThreatsX, ThreatSpots, -0.7f,
                                      "DRONE", "TURRET", "PIT", "ENERGY CELL", "BARRIER", "WAY OUT"); // no X: it reads as H in Kenney Future
            var reactorLabels = Labels(canvas, "ReactorLabels", ReactorX, ReactorX, new[] { -1.5f, 4.5f }, 1.2f, "CALM", "OVERLOAD");
            var ui = InterfaceOverlay(canvas, plain);

            // Titles, caption, page and navigation.
            var subtitle = NewLabel("Subtitle", canvas, "", 13, EchoPalette.Energy, TextAnchor.MiddleLeft);
            Place(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -22f), new Vector2(600f, 20f));
            var title = NewLabel("Title", canvas, "", 28, Color.white, TextAnchor.MiddleLeft);
            Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -42f), new Vector2(760f, 40f));
            var page = NewLabel("Page", canvas, "1 / 1", 13, Dim, TextAnchor.MiddleRight);
            Place(page.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -24f), new Vector2(120f, 20f));

            var captionPanel = NewPanel("CaptionPanel", canvas, new Vector2(860f, 70f));
            Place(captionPanel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(860f, 70f));
            var caption = NewLabel("Caption", captionPanel, "", 15, CaptionColor, TextAnchor.MiddleLeft);
            caption.font = plain;
            caption.horizontalOverflow = HorizontalWrapMode.Wrap;
            caption.lineSpacing = 1.1f;
            Place(caption.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(812f, 60f));

            var hint = NewLabel("Hint", canvas, "A / D  BROWSE     ESC  MENU", 10, Dim, TextAnchor.MiddleRight);
            Place(hint.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -84f), new Vector2(300f, 16f));

            var previous = ArrowButton(canvas, "Previous", true, new Vector2(0f, 0.5f), new Vector2(28f, 20f));
            var next = ArrowButton(canvas, "Next", false, new Vector2(1f, 0.5f), new Vector2(-28f, 20f));
            var menu = TextButton(canvas, "Menu", "MENU", new Vector2(1f, 1f), new Vector2(-40f, -48f));

            var showcase = canvas.gameObject.AddComponent<DesignShowcase>();
            EchoPrefabFactory.SetReference(showcase, "worldCamera", camera);
            EchoPrefabFactory.SetReference(showcase, "globalLight", globalLight);
            EchoPrefabFactory.SetReference(showcase, "titleText", title);
            EchoPrefabFactory.SetReference(showcase, "subtitleText", subtitle);
            EchoPrefabFactory.SetReference(showcase, "captionText", caption);
            EchoPrefabFactory.SetReference(showcase, "pageText", page);
            EchoPrefabFactory.SetReference(showcase, "dimmer", dimmer);
            EchoPrefabFactory.SetReference(showcase, "previousButton", previous);
            EchoPrefabFactory.SetReference(showcase, "nextButton", next);
            EchoPrefabFactory.SetReference(showcase, "menuButton", menu);

            var slides = new List<SlideData>
            {
                new SlideData("PHASE 3 · DESIGN", "VISUAL DESIGN",
                    "A dark sci-fi facility told in one neon language. Pixel art at 32 px per unit, rendered at 640×360 and scaled up sharp to any screen.",
                    LabX, lab, MusicId.Menu, intro, 0.72f),
                new SlideData("ART DIRECTION", "COLOR LANGUAGE",
                    "Every color has one job, so any screen reads at a glance. Cyan is energy, warm red is danger, green is the way out. Nothing friendly is ever warm.",
                    LabX, lab, MusicId.Menu, palette, 0.88f),
                new SlideData("CHARACTER", "THE HERO",
                    "Recolored from Ansimuz's Warped City hero: a white jacket with cool shading (yellow would read as danger), magenta hair and a cyan accent. Squash and stretch on every jump and landing.",
                    HeroX, lab, MusicId.Lab, heroLabels, 0f),
                new SlideData("GAMEPLAY OBJECTS", "THREATS AND TOOLS",
                    "Danger is always warm: pits glow red and enemy eyes burn red. Energy cells and the barriers a dash breaks are cyan; the exit door is green. Drones and turrets guard the security wing, and a dash destroys them.",
                    ThreatsX, lab, MusicId.Lab, threatLabels, 0f),
                new SlideData("SECTOR 1", lab.displayName, lab.description, LabX, lab, lab.music, null, 0f),
                new SlideData("SECTOR 2", security.displayName, security.description, SecurityX, security, security.music, null, 0f),
                new SlideData("SECTOR 3", core.displayName, core.description, CoreX, core, core.music, null, 0f),
                new SlideData("FINAL ARENA", reactor.displayName, reactor.description, ReactorX, reactor, reactor.music, reactorLabels, 0f),
                new SlideData("UI", "INTERFACE",
                    "Drawn in the same palette: dark translucent panels, white Kenney Future text, cyan for anything energy. The meter shows exactly how many dashes are left.",
                    LabX, lab, MusicId.Menu, ui, 0.88f),
            };
            WriteSlides(showcase, slides);
        }

        sealed class SlideData
        {
            public readonly string Subtitle, Title, Caption;
            public readonly float CameraX, Dim;
            public readonly EnvironmentKit Kit;
            public readonly MusicId Music;
            public readonly GameObject Overlay;

            public SlideData(string subtitle, string title, string caption, float cameraX, EnvironmentKit kit, MusicId music,
                             GameObject overlay, float dim)
            {
                Subtitle = subtitle;
                Title = title;
                Caption = caption;
                CameraX = cameraX;
                Kit = kit;
                Music = music;
                Overlay = overlay;
                Dim = dim;
            }
        }

        static void WriteSlides(DesignShowcase showcase, List<SlideData> slides)
        {
            var settings = new SerializedObject(showcase);
            var array = settings.FindProperty("slides");
            array.arraySize = slides.Count;
            for (int i = 0; i < slides.Count; i++)
            {
                var slide = slides[i];
                var element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("title").stringValue = slide.Title;
                element.FindPropertyRelative("subtitle").stringValue = slide.Subtitle;
                element.FindPropertyRelative("caption").stringValue = slide.Caption;
                element.FindPropertyRelative("cameraX").floatValue = slide.CameraX;
                element.FindPropertyRelative("voidColor").colorValue = slide.Kit.voidColor;
                element.FindPropertyRelative("ambientColor").colorValue = slide.Kit.ambientColor;
                element.FindPropertyRelative("ambientIntensity").floatValue = slide.Kit.ambientIntensity;
                element.FindPropertyRelative("music").enumValueIndex = (int)slide.Music;
                element.FindPropertyRelative("overlay").objectReferenceValue = slide.Overlay;
                element.FindPropertyRelative("dim").floatValue = slide.Dim;
                if (slide.Overlay != null)
                    slide.Overlay.SetActive(false);
            }
            settings.FindProperty("cameraY").floatValue = CameraY;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        // Labels under objects in a world section, placed where the camera shows them.
        static GameObject Labels(RectTransform canvas, string name, float sectionX, float cameraX, float[] spots, float worldY,
                                 params string[] texts)
        {
            var root = NewRect(name, canvas);
            Stretch(root);
            for (int i = 0; i < texts.Length; i++)
            {
                float x = (sectionX + spots[i] - cameraX) * CanvasPerUnit;
                float y = (worldY - CameraY) * CanvasPerUnit;
                var label = NewLabel(texts[i], root, texts[i], 13, Color.white, TextAnchor.MiddleCenter);
                Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(160f, 20f));
            }
            return root.gameObject;
        }

        static GameObject IntroOverlay(RectTransform canvas, Font plain)
        {
            var root = NewRect("IntroOverlay", canvas);
            Stretch(root);
            var logoSprite = UiSprite("Logo");
            var logo = NewImage("Logo", root, logoSprite, Color.white);
            Place(logo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), PixelSize(logoSprite) * (4f / 3f));
            var tagline = NewLabel("Tagline", root, "EVERY DASH IS A DECISION", 16, EchoPalette.Energy, TextAnchor.MiddleCenter);
            Place(tagline.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -32f), new Vector2(600f, 24f));

            string[] rules =
            {
                "Dark navy world, strong silhouettes",
                "Cyan = energy · warm red = danger · green = exit",
                "White interface over translucent panels",
            };
            for (int i = 0; i < rules.Length; i++)
            {
                var line = NewLabel("Rule", root, rules[i], 15, CaptionColor, TextAnchor.MiddleCenter);
                line.font = plain;
                Place(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -72f - i * 24f), new Vector2(600f, 22f));
            }
            return root.gameObject;
        }

        static GameObject PaletteOverlay(RectTransform canvas, Font plain)
        {
            var root = NewRect("PaletteOverlay", canvas);
            Stretch(root);
            (string name, Color color, string role)[] swatches =
            {
                ("VOID", new Color32(0x07, 0x0A, 0x12, 0xFF), "Backgrounds and shadows"),
                ("STRUCTURE", new Color32(0x1B, 0x2A, 0x35, 0xFF), "Walls and floors, never glowing"),
                ("HERO", new Color32(0xF1, 0xF5, 0xFF, 0xFF), "The brightest silhouette on screen"),
                ("ENERGY", EchoPalette.Energy, "Cells, the meter, dashes, breakable barriers"),
                ("DANGER", new Color32(0xFF, 0x5A, 0x36, 0xFF), "Pits, lasers, enemy eyes, the boss"),
                ("WAY OUT", EchoPalette.Exit, "Exit doors and goal lights"),
            };
            const float spacing = 132f;
            float start = -spacing * (swatches.Length - 1) * 0.5f;
            for (int i = 0; i < swatches.Length; i++)
            {
                float x = start + i * spacing;
                var frame = NewPanel("Swatch_" + swatches[i].name, root, new Vector2(108f, 96f));
                Place(frame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 50f), new Vector2(108f, 96f));
                var fill = NewImage("Color", frame, EchoSpriteFactory.Load(EchoSpriteFactory.Square), swatches[i].color);
                Place(fill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84f, 72f));

                var name = NewLabel("Name", root, swatches[i].name, 14, Color.white, TextAnchor.MiddleCenter);
                Place(name.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, -12f), new Vector2(136f, 20f));
                var hex = NewLabel("Hex", root, "#" + ColorUtility.ToHtmlStringRGB(swatches[i].color), 11, Dim, TextAnchor.MiddleCenter);
                Place(hex.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, -32f), new Vector2(136f, 16f));
                var role = NewLabel("Role", root, swatches[i].role, 13, CaptionColor, TextAnchor.UpperCenter);
                role.font = plain;
                role.horizontalOverflow = HorizontalWrapMode.Wrap;
                Place(role.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(x, -46f), new Vector2(128f, 60f));
            }
            return root.gameObject;
        }

        static GameObject InterfaceOverlay(RectTransform canvas, Font plain)
        {
            var root = NewRect("InterfaceOverlay", canvas);
            Stretch(root);

            // Energy meter: three charges left, then empty (the bolt turns red).
            Meter(root, new Vector2(-300f, 70f), 3, EchoPalette.Energy);
            Meter(root, new Vector2(-300f, -30f), 0, EchoPalette.Hazard);
            Caption(root, plain, "Energy meter: one cell per dash", new Vector2(-300f, 20f));
            Caption(root, plain, "Empty: the bolt blinks red", new Vector2(-300f, -80f));

            // A menu, as drawn: the selected entry gets the bar and the chevron.
            var menu = NewPanel("Menu", root, new Vector2(240f, 156f));
            Place(menu, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(240f, 156f));
            string[] entries = { "RESUME", "RESTART", "MAIN MENU" };
            for (int i = 0; i < entries.Length; i++)
            {
                var row = NewRect(entries[i], menu);
                Place(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f - i * 40f), new Vector2(204f, 32f));
                if (i == 0)
                {
                    var bar = NewImage("Highlight", row, UiSprite("SelectBar"), Color.white);
                    bar.type = Image.Type.Sliced;
                    Stretch(bar.rectTransform);
                    var chevron = NewImage("Chevron", row, UiSprite("Chevron"), Color.white);
                    Place(chevron.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), PixelSize(chevron.sprite));
                }
                var text = NewLabel("Label", row, entries[i], 16, i == 0 ? Color.white : Dim, TextAnchor.MiddleCenter);
                Stretch(text.rectTransform);
            }
            Caption(root, plain, "Menus: keyboard, gamepad, mouse and touch", new Vector2(0f, -70f));

            // Level cards.
            var sector = NewLabel("Sector", root, "SECTOR 01", 13, EchoPalette.Energy, TextAnchor.MiddleCenter);
            Place(sector.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 92f), new Vector2(260f, 18f));
            var levelTitle = NewLabel("LevelTitle", root, "ABANDONED LAB", 22, Color.white, TextAnchor.MiddleCenter);
            Place(levelTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 66f), new Vector2(300f, 30f));
            var underline = NewImage("Underline", root, EchoSpriteFactory.Load(EchoSpriteFactory.Square), WithAlpha(EchoPalette.Energy, 0.7f));
            Place(underline.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 46f), new Vector2(200f, 3f));
            var cleared = NewLabel("Cleared", root, "SECTOR CLEARED", 20, EchoPalette.Exit, TextAnchor.MiddleCenter);
            Place(cleared.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, -14f), new Vector2(300f, 28f));
            Caption(root, plain, "Level title and level-complete cards", new Vector2(300f, -70f));
            return root.gameObject;
        }

        static void Meter(RectTransform parent, Vector2 position, int full, Color boltColor)
        {
            var panel = NewImage("Meter", parent, UiSprite("Panel"), Color.white);
            panel.type = Image.Type.Sliced;
            const float px = 3f;
            Vector2 cell = PixelSize(UiSprite("CellEmpty"));
            Vector2 bolt = PixelSize(UiSprite("Bolt"));
            float width = 5 * px + bolt.x + 2 * px + 5 * cell.x + 4 * 2 * px + 5 * px;
            Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(width, cell.y + 8 * px));

            float x = 5 * px;
            var boltImage = NewImage("Bolt", panel.rectTransform, UiSprite("Bolt"), boltColor);
            Place(boltImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(x, 0f), bolt);
            x += bolt.x + 2 * px;
            for (int i = 0; i < 5; i++)
            {
                var image = NewImage("Cell", panel.rectTransform, UiSprite(i < full ? "CellFull" : "CellEmpty"), Color.white);
                Place(image.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(x, 0f), cell);
                x += cell.x + 2 * px;
            }
        }

        static void Caption(RectTransform parent, Font plain, string text, Vector2 position)
        {
            var label = NewLabel("Caption", parent, text, 13, CaptionColor, TextAnchor.MiddleCenter);
            label.font = plain;
            Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(300f, 20f));
        }

        // The menu chevron at double size; the "previous" one is mirrored.
        static Button ArrowButton(RectTransform canvas, string name, bool pointsLeft, Vector2 anchor, Vector2 position)
        {
            var panel = NewPanel(name, canvas, new Vector2(48f, 64f));
            Place(panel, anchor, new Vector2(anchor.x, 0.5f), position, new Vector2(48f, 64f));
            panel.GetComponent<Image>().raycastTarget = true;
            var chevron = NewImage("Chevron", panel, UiSprite("Chevron"), Color.white);
            Place(chevron.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(pointsLeft ? -2f : 2f, 0f),
                  PixelSize(chevron.sprite) * 2f);
            chevron.rectTransform.localScale = new Vector3(pointsLeft ? -1f : 1f, 1f, 1f);
            return ButtonOn(panel);
        }

        static Button TextButton(RectTransform canvas, string name, string text, Vector2 anchor, Vector2 position)
        {
            var panel = NewPanel(name, canvas, new Vector2(84f, 30f));
            Place(panel, anchor, anchor, position, new Vector2(84f, 30f));
            panel.GetComponent<Image>().raycastTarget = true;
            var label = NewLabel("Label", panel, text, 12, Color.white, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            return ButtonOn(panel);
        }

        static Button ButtonOn(RectTransform panel)
        {
            var button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = panel.GetComponent<Image>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.7f, 0.95f, 1f);
            colors.pressedColor = EchoPalette.Energy;
            colors.selectedColor = Color.white;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }
    }
}
