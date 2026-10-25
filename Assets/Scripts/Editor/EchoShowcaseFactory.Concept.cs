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
    /// The concept deck (presentation phases 1 and 2): the pitch, core loop, story, platform and
    /// scope, then the diagrams (architecture, classes, the two state machines, scene flow and
    /// the energy economy), which only appear from phase 2. Diagrams are drawn from UI panels,
    /// labels and arrows, so they stay sharp at any screen size; Docs/DIAGRAMS.md has the same
    /// diagrams as text.
    /// </summary>
    public static partial class EchoShowcaseFactory
    {
        public static string ConceptPath => $"{EchoSceneFactory.ScenesFolder}/Presentation/{SceneNames.ConceptShowcase}.unity";

        static readonly Color Ink = new Color(0.85f, 0.92f, 0.96f);
        static readonly Color Line = new Color(0.45f, 0.75f, 0.85f);
        static readonly Color Good = new Color(0.35f, 1f, 0.6f);
        static readonly Color Bad = new Color(1f, 0.45f, 0.35f);
        static readonly Color Note = new Color(0.6f, 0.7f, 0.78f);
        static Font plainFont;

        [MenuItem("ECHO/Setup/Rebuild Concept Showcase")]
        public static void RebuildConcept()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EchoSceneFactory.EnsureScene(ConceptPath, BuildConcept, true);
            EchoProjectSetup.SetupBuildScenes();
        }

        public static void EnsureConcept() => EchoSceneFactory.EnsureScene(ConceptPath, BuildConcept, false);

        static void BuildConcept()
        {
            var lab = EchoArtFactory.LoadKit(Biome.Lab);
            var core = EchoArtFactory.LoadKit(Biome.Core);
            var reactor = EchoArtFactory.LoadKit(Biome.Reactor);
            plainFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var camera = EchoSceneFactory.CreateCamera(lab.voidColor);
            camera.transform.position = new Vector3(LabX, CameraY, -10f);
            EchoLevelArt.DustMotes(camera.transform);
            var globalLight = EchoLevelArt.CreateGlobalLight(lab.ambientIntensity, lab.ambientColor);

            var world = new GameObject("World").transform;
            HeroStudio(Section(world, "HeroStudio"), lab, HeroX);
            LabSet(Section(world, "Lab"), lab, LabX);
            ReactorSet(Section(world, "Reactor"), reactor, ReactorX);

            var showcase = Deck("ConceptCanvas", camera, globalLight, lab.voidColor, out var overlays);
            const PresentationPhase one = PresentationPhase.Conception;
            const PresentationPhase two = PresentationPhase.Diagrams;
            var slides = new List<SlideData>
            {
                new SlideData("PHASE 1 · CONCEPTION", "ECHO",
                    "A short sci-fi platformer for the browser, built around one idea: the hero's best move costs energy, and energy is scarce.",
                    LabX, lab, MusicId.Menu, TitleOverlay(overlays), 0.72f, one),
                new SlideData("THE PITCH", "EVERY DASH IS A DECISION",
                    "The dash crosses wide gaps, breaks energy barriers and destroys machines. But every dash spends a charge, the suit holds three, and every sector ends at a door that only a dash can open. Spend it now, or keep it?",
                    HeroX, lab, MusicId.Lab, null, 0f, one),
                new SlideData("WHAT THE PLAYER DOES", "CORE LOOP",
                    "Each sector repeats the loop with something new to spend a charge on: barriers, wide pits, drones and turrets, then locks that want charges held instead of spent.",
                    LabX, lab, MusicId.Lab, CoreLoopOverlay(overlays), 0.9f, one),
                new SlideData("STORY", "THE PREMISE",
                    "She wakes alone in a cryo pod. The research facility is dark but its security still runs. Far below, the reactor that keeps everything alive is failing, guarded by the machine built to protect it. Calm the core, and the facility can call for help.",
                    ReactorX, reactor, MusicId.Core, null, 0.25f, one),
                new SlideData("TARGET PLATFORM", "BROWSER AND PHONE",
                    "Made in Unity 6 for WebGL and published on GamePix, a web games portal: no install, one link, played on desktop and on phones.",
                    LabX, lab, MusicId.Menu, PlatformOverlay(overlays), 0.9f, one),
                new SlideData("SCOPE", "WHAT IS IN, WHAT IS OUT",
                    "A small game finished well: one mechanic explored deeply, rather than many half-built systems.",
                    LabX, lab, MusicId.Menu, ScopeOverlay(overlays), 0.9f, one),

                new SlideData("PHASE 2 · DIAGRAMS", "ARCHITECTURE",
                    "Persistent systems live for the whole session; one scene is loaded at a time. Gameplay talks to the systems through static events, so levels never hold references to the menus, audio or ads.",
                    LabX, lab, MusicId.Menu, ArchitectureOverlay(overlays), 0.92f, two),
                new SlideData("CODE", "CLASSES",
                    "Three small interfaces carry the rules: anything a dash can hit, anything a checkpoint can bring back, and anything a console can switch.",
                    LabX, lab, MusicId.Menu, ClassesOverlay(overlays), 0.92f, two),
                new SlideData("STATE MACHINE", "GAME STATES",
                    "GameManager holds one state for the whole game; the HUD, pause menu and input read it. There is no game over: a death goes back to the last checkpoint.",
                    LabX, lab, MusicId.Menu, GameStatesOverlay(overlays), 0.92f, two),
                new SlideData("STATE MACHINE", "THE GUARDIAN",
                    "The boss loops attack, then an open window. Six hits in three phases; each phase opens for less time. A death restarts the current phase, never the whole fight.",
                    ReactorX, reactor, MusicId.Boss, GuardianOverlay(overlays), 0.88f, two),
                new SlideData("SCENES", "SCENE FLOW",
                    "Boot picks the first scene for the presentation phase. Each level leads to the next only if the phase presents it; otherwise it returns to the menu.",
                    LabX, lab, MusicId.Menu, SceneFlowOverlay(overlays), 0.92f, two),
                new SlideData("BALANCE", "ENERGY ECONOMY",
                    "Every sector gives a little more energy than it needs, so one mistake is forgiven but waste is not. The meter never holds more than three charges.",
                    LabX, core, MusicId.Menu, EconomyOverlay(overlays), 0.92f, two),
            };
            WriteSlides(showcase, slides);
        }

        // --- phase 1 overlays -------------------------------------------------------------------

        static GameObject TitleOverlay(RectTransform parent)
        {
            var root = Layer(parent, "TitleOverlay");
            var logoSprite = UiSprite("Logo");
            var logo = NewImage("Logo", root, logoSprite, Color.white);
            Place(logo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), PixelSize(logoSprite) * (4f / 3f));
            var tagline = NewLabel("Tagline", root, "EVERY DASH IS A DECISION", 16, EchoPalette.Energy, TextAnchor.MiddleCenter);
            Place(tagline.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -32f), new Vector2(600f, 24f));
            string[] lines =
            {
                "A 2D action platformer · Unity 6 · WebGL on GamePix",
                "Three sectors, a guardian, and a way home",
                "One mechanic, explored deeply: energy",
            };
            for (int i = 0; i < lines.Length; i++)
                Words(root, lines[i], new Vector2(0f, -72f - i * 24f), new Vector2(640f, 22f), 15, Ink, TextAnchor.MiddleCenter);
            return root.gameObject;
        }

        static GameObject CoreLoopOverlay(RectTransform parent)
        {
            var root = Layer(parent, "CoreLoopOverlay");
            var size = new Vector2(330f, 92f);
            Box(root, "1  EXPLORE", "Run and jump through the sector. Collect energy cells: one charge each, three at most.",
                new Vector2(-235f, 92f), size, EchoPalette.Energy);
            Box(root, "2  DECIDE", "A gap, a barrier, a machine, a lock. Dash now, or keep the charge for later?",
                new Vector2(235f, 92f), size, Ink);
            Box(root, "3  SPEND OR HOLD", "A dash crosses wide gaps, breaks barriers and destroys machines. Charges held open core locks.",
                new Vector2(235f, -88f), size, Ink);
            Box(root, "4  THE EXIT", "Every sector ends at a door only a dash can break. Arrive with no charge and you go back.",
                new Vector2(-235f, -88f), size, Good);
            Arrow(root, "", new Vector2(-70f, 92f), new Vector2(70f, 92f));
            Arrow(root, "", new Vector2(235f, 46f), new Vector2(235f, -42f));
            Arrow(root, "", new Vector2(70f, -88f), new Vector2(-70f, -88f));
            ArrowLabelAt(root, "next sector", new Vector2(-170f, 2f), new Vector2(-235f, -42f), new Vector2(-235f, 46f));
            var heart = NewLabel("Heart", root, "EVERY DASH IS A DECISION", 13, EchoPalette.Energy, TextAnchor.MiddleCenter);
            Place(heart.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(300f, 20f));
            return root.gameObject;
        }

        static GameObject PlatformOverlay(RectTransform parent)
        {
            var root = Layer(parent, "PlatformOverlay");
            var size = new Vector2(250f, 160f);
            Box(root, "ON THE WEB",
                "A Unity 6 WebGL build, packaged by the GamePix plugin. Opens from one link, no install. An ad may show between levels, never during play.",
                new Vector2(-265f, 20f), size, EchoPalette.Energy);
            Box(root, "THREE WAYS TO PLAY",
                "Keyboard (A/D, Space, Shift), any gamepad, or on-screen touch buttons on phones. Hints name the buttons of the device in use.",
                new Vector2(0f, 20f), size, EchoPalette.Energy);
            Box(root, "SHORT SESSIONS",
                "A sector takes a few minutes. Checkpoints, an instant return to the last one (R), and Continue from the menu.",
                new Vector2(265f, 20f), size, EchoPalette.Energy);
            return root.gameObject;
        }

        static GameObject ScopeOverlay(RectTransform parent)
        {
            var root = Layer(parent, "ScopeOverlay");
            var size = new Vector2(370f, 150f);
            Box(root, "IN",
                "• Three sectors, a boss fight and an ending\n• One mechanic explored deeply: energy and the dash\n• Hand-built levels with checkpoints and hints\n• Pixel art: original sprites plus free (CC0 and similar) packs\n• Keyboard, gamepad and touch",
                new Vector2(-195f, 20f), size, Good);
            Box(root, "OUT",
                "• Multiplayer\n• Procedural generation\n• RPG systems (stats, loot, levelling up)\n• Analytics and tracking SDKs\n• Anything that needs a server",
                new Vector2(195f, 20f), size, Bad);
            return root.gameObject;
        }

        // --- phase 2 overlays -------------------------------------------------------------------
        // Diagram content stays inside x -400..400, clear of the deck's arrow buttons.

        static GameObject ArchitectureOverlay(RectTransform parent)
        {
            var root = Layer(parent, "ArchitectureOverlay");
            Box(root, "BOOT", "Picks the first scene for the presentation phase.",
                new Vector2(-310f, 125f), new Vector2(180f, 92f), Note);
            Box(root, "SYSTEMS  (persistent, one copy)",
                "GameManager (state) · SceneLoader (fades) · AudioManager · VfxManager · GamePixManager · gameplay UI: HUD, cards, hints, pause, touch buttons",
                new Vector2(0f, 125f), new Vector2(360f, 92f), EchoPalette.Energy);
            Box(root, "PRESENTATION", "PresentationConfig: which phase is shown and which features are on.",
                new Vector2(310f, 125f), new Vector2(180f, 92f), Note);
            Box(root, "SCENES  (one at a time)",
                "Menu, levels, the arena, the ending, the showcases. A level: LevelController, the player, and its cells, barriers, machines, consoles, plates and locks.",
                new Vector2(0f, -25f), new Vector2(360f, 100f), Ink);
            Box(root, "DATA", "ScriptableObjects: PlayerTuning, EnvironmentKits, AudioLibrary, VfxLibrary.",
                new Vector2(-310f, -110f), new Vector2(180f, 100f), Note);
            Box(root, "EDITOR FACTORIES", "Code that builds every scene, prefab and asset (ECHO > Setup).",
                new Vector2(310f, -110f), new Vector2(180f, 100f), Note);
            Arrow(root, "", new Vector2(-220f, 125f), new Vector2(-180f, 125f));
            Arrow(root, "", new Vector2(220f, 125f), new Vector2(180f, 125f));
            ArrowLabelAt(root, "static events", new Vector2(-100f, 54f), new Vector2(-40f, 25f), new Vector2(-40f, 79f));
            ArrowLabelAt(root, "the current level", new Vector2(110f, 54f), new Vector2(40f, 79f), new Vector2(40f, 25f));
            ArrowLabelAt(root, "read", new Vector2(-235f, -45f), new Vector2(-220f, -95f), new Vector2(-180f, -60f));
            ArrowLabelAt(root, "build", new Vector2(235f, -45f), new Vector2(220f, -95f), new Vector2(180f, -60f));
            return root.gameObject;
        }

        static GameObject ClassesOverlay(RectTransform parent)
        {
            var root = Layer(parent, "ClassesOverlay");
            var size = new Vector2(330f, 86f);
            Box(root, "PlayerController",
                "Reads input and drives PlayerMovement (run, jump, coyote time, buffers), PlayerDash (dash, hits) and EnergySystem (charges).",
                new Vector2(-200f, 128f), size, EchoPalette.Energy);
            Box(root, "LevelController",
                "Spawn, deaths, checkpoints, completion and stats. A checkpoint snapshots which IRestorables are spent.",
                new Vector2(200f, 128f), size, EchoPalette.Energy);
            Box(root, "«interface» IDashTarget", "OnDashHit(dash)\nBreakableBarrier · Machine · Guardian",
                new Vector2(-200f, 0f), size, Ink);
            Box(root, "«interface» IRestorable", "IsSpent · Restore()\nEnergyPickup · BreakableBarrier · Machine · ControlConsole · CollapsingPlatform · Guardian",
                new Vector2(200f, 0f), size, Ink);
            Box(root, "Machine  (abstract)", "Touch kills, a dash destroys.\nDrone (patrols) · Turret (fires bolts)",
                new Vector2(-200f, -124f), size, Bad);
            Box(root, "«interface» ISwitchable", "SetSwitched(on)\nSecurityDoor · LaserGate, switched by a ControlConsole",
                new Vector2(200f, -124f), size, Ink);
            ArrowLabelAt(root, "dash hits", new Vector2(-140f, 64f), new Vector2(-200f, 85f), new Vector2(-200f, 43f));
            ArrowLabelAt(root, "restores on respawn", new Vector2(285f, 64f), new Vector2(200f, 85f), new Vector2(200f, 43f));
            ArrowLabelAt(root, "implements", new Vector2(-130f, -62f), new Vector2(-200f, -81f), new Vector2(-200f, -43f));
            return root.gameObject;
        }

        static GameObject GameStatesOverlay(RectTransform parent)
        {
            var root = Layer(parent, "GameStatesOverlay");
            var size = new Vector2(140f, 46f);
            State(root, "BOOT", new Vector2(-330f, 125f), size, Note);
            State(root, "MAIN MENU", new Vector2(-150f, 125f), size, Ink);
            State(root, "PLAYING", new Vector2(60f, 125f), size, EchoPalette.Energy);
            State(root, "LEVEL COMPLETE", new Vector2(300f, 125f), new Vector2(170f, 46f), Good);
            State(root, "PAUSED", new Vector2(60f, -40f), size, Ink);
            State(root, "ENDING", new Vector2(300f, -40f), new Vector2(170f, 46f), Good);
            Arrow(root, "", new Vector2(-260f, 125f), new Vector2(-220f, 125f));
            ArrowLabelAt(root, "Play / Continue", new Vector2(-35f, 152f), new Vector2(-80f, 125f), new Vector2(-10f, 125f));
            ArrowLabelAt(root, "exit door", new Vector2(172f, 152f), new Vector2(130f, 135f), new Vector2(215f, 135f));
            ArrowLabelAt(root, "next level", new Vector2(172f, 98f), new Vector2(215f, 115f), new Vector2(130f, 115f));
            ArrowLabelAt(root, "Esc", new Vector2(20f, 40f), new Vector2(45f, 102f), new Vector2(45f, -17f));
            ArrowLabelAt(root, "resume", new Vector2(112f, 40f), new Vector2(75f, -17f), new Vector2(75f, 102f));
            ArrowLabelAt(root, "after the Guardian", new Vector2(225f, 40f), new Vector2(300f, 102f), new Vector2(300f, -17f));
            ArrowLabelAt(root, "a press", new Vector2(120f, -82f), new Vector2(300f, -63f), new Vector2(300f, -95f),
                         new Vector2(-150f, -95f), new Vector2(-150f, 102f));
            Words(root, "Death: back to the last checkpoint at once (no game over).  Quit from pause: back to the menu.",
                  new Vector2(0f, -130f), new Vector2(780f, 22f), 13, Note, TextAnchor.MiddleCenter);
            return root.gameObject;
        }

        static GameObject GuardianOverlay(RectTransform parent)
        {
            var root = Layer(parent, "GuardianOverlay");
            var size = new Vector2(150f, 50f);
            State(root, "ASLEEP", new Vector2(-320f, 125f), size, Note);
            State(root, "ATTACK", new Vector2(-100f, 125f), size, Bad);
            State(root, "OPEN", new Vector2(120f, 125f), size, EchoPalette.Energy);
            State(root, "ENRAGE", new Vector2(320f, 125f), size, Bad);
            State(root, "DEFEATED", new Vector2(120f, -30f), size, Good);
            ArrowLabelAt(root, "wakes", new Vector2(-210f, 152f), new Vector2(-245f, 125f), new Vector2(-175f, 125f));
            ArrowLabelAt(root, "sinks, shell opens", new Vector2(10f, 165f), new Vector2(-25f, 137f), new Vector2(45f, 137f));
            ArrowLabelAt(root, "window ends, or a hit", new Vector2(10f, 85f), new Vector2(45f, 113f), new Vector2(-25f, 113f));
            ArrowLabelAt(root, "2nd hit", new Vector2(220f, 152f), new Vector2(195f, 125f), new Vector2(245f, 125f));
            ArrowLabelAt(root, "next phase", new Vector2(250f, 68f), new Vector2(320f, 100f), new Vector2(320f, 55f),
                         new Vector2(-100f, 55f), new Vector2(-100f, 100f));
            ArrowLabelAt(root, "6th hit", new Vector2(160f, 20f), new Vector2(120f, 100f), new Vector2(120f, -5f));
            Words(root, "Dash into OPEN: a hit.  Into the closed shell: it bounces off and the charge is lost.  Touch without dashing: death.",
                  new Vector2(0f, -88f), new Vector2(800f, 20f), 13, Ink, TextAnchor.MiddleCenter);
            Words(root, "Phase 1: bolts or a floor wave, open 2.6 s · Phase 2: + aimed ceiling beams, 2.2 s · Phase 3: both walls at once, 1.8 s",
                  new Vector2(0f, -114f), new Vector2(800f, 20f), 13, Note, TextAnchor.MiddleCenter);
            Words(root, "If the player dies, the current phase restarts; hits from earlier phases stay.",
                  new Vector2(0f, -138f), new Vector2(800f, 20f), 13, Note, TextAnchor.MiddleCenter);
            return root.gameObject;
        }

        static GameObject SceneFlowOverlay(RectTransform parent)
        {
            var root = Layer(parent, "SceneFlowOverlay");
            var size = new Vector2(130f, 46f);
            State(root, "BOOT", new Vector2(-316f, 125f), size, Note);
            State(root, "MAIN MENU", new Vector2(-158f, 125f), size, Ink);
            State(root, "LEVEL 1", new Vector2(0f, 125f), size, EchoPalette.Energy);
            State(root, "LEVEL 2", new Vector2(158f, 125f), size, EchoPalette.Energy);
            State(root, "LEVEL 3", new Vector2(316f, 125f), size, EchoPalette.Energy);
            State(root, "THE GUARDIAN", new Vector2(316f, -10f), new Vector2(150f, 46f), Bad);
            State(root, "ENDING", new Vector2(110f, -10f), size, Good);
            State(root, "CONCEPT DECK", new Vector2(-316f, -10f), new Vector2(150f, 46f), Note);
            State(root, "DESIGN DECK", new Vector2(-316f, -110f), new Vector2(150f, 46f), Note);
            Arrow(root, "", new Vector2(-251f, 125f), new Vector2(-223f, 125f));
            Arrow(root, "", new Vector2(-93f, 125f), new Vector2(-65f, 125f));
            Arrow(root, "", new Vector2(65f, 125f), new Vector2(93f, 125f));
            ArrowLabelAt(root, "phase 5+", new Vector2(237f, 160f), new Vector2(223f, 125f), new Vector2(251f, 125f));
            ArrowLabelAt(root, "phase 6+", new Vector2(270f, 57f), new Vector2(316f, 102f), new Vector2(316f, 13f));
            ArrowLabelAt(root, "phase 7+", new Vector2(208f, 14f), new Vector2(241f, -10f), new Vector2(175f, -10f));
            ArrowLabelAt(root, "the end", new Vector2(-15f, -60f), new Vector2(110f, -33f), new Vector2(110f, -70f),
                         new Vector2(-120f, -70f), new Vector2(-120f, 102f));
            ArrowLabelAt(root, "phases 1-2", new Vector2(-262f, 57f), new Vector2(-316f, 102f), new Vector2(-316f, 13f));
            ArrowLabelAt(root, "phase 3", new Vector2(-370f, -60f), new Vector2(-391f, 115f), new Vector2(-400f, 115f),
                         new Vector2(-400f, -110f), new Vector2(-391f, -110f));
            Words(root, "Phases 4-8 start at the main menu. A level whose phase isn't presented yet returns to the menu.",
                  new Vector2(60f, -140f), new Vector2(640f, 20f), 13, Note, TextAnchor.MiddleCenter);
            return root.gameObject;
        }

        static GameObject EconomyOverlay(RectTransform parent)
        {
            var root = Layer(parent, "EconomyOverlay");
            string[] header = { "SECTOR", "START", "CELLS", "DASHES NEEDED", "ALSO NEEDS", "SPARE" };
            string[][] rows =
            {
                new[] { "1 · Awakening", "0", "5", "4 (barrier, 2 pits, exit)", "–", "1" },
                new[] { "2 · Security Wing", "1", "3", "3 (2 pits, exit)", "machines: optional targets", "1" },
                new[] { "3 · Energy Core", "0", "9", "4 (3 pits, exit)", "hold 2, then a full meter", "1" },
                new[] { "The Guardian", "1", "3, back in 4 s", "7 (6 hits, exit)", "hits only while open", "–" },
            };
            float[] columns = { -320f, -205f, -120f, 25f, 205f, 350f };
            float[] widths = { 150f, 50f, 110f, 170f, 180f, 60f };
            Panel(root, new Vector2(0f, 20f), new Vector2(820f, 250f));
            for (int c = 0; c < header.Length; c++)
                Words(root, header[c], new Vector2(columns[c], 112f), new Vector2(widths[c], 22f), 13, EchoPalette.Energy, TextAnchor.MiddleCenter, true);
            for (int r = 0; r < rows.Length; r++)
            {
                for (int c = 0; c < rows[r].Length; c++)
                    Words(root, rows[r][c], new Vector2(columns[c], 64f - r * 46f), new Vector2(widths[c], 40f), 13, Ink, TextAnchor.MiddleCenter);
            }
            Words(root, "The meter holds 3 charges at most. A dash costs 1; a cell gives 1.",
                  new Vector2(0f, -132f), new Vector2(780f, 20f), 13, Note, TextAnchor.MiddleCenter);
            return root.gameObject;
        }

        // --- diagram pieces ---------------------------------------------------------------------

        static RectTransform Layer(RectTransform parent, string name)
        {
            var root = NewRect(name, parent);
            Stretch(root);
            return root;
        }

        static void Panel(RectTransform parent, Vector2 center, Vector2 size)
        {
            var panel = NewImage("Panel", parent, UiSprite("Panel"), Color.white);
            panel.type = Image.Type.Sliced;
            Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), center, size);
        }

        static Text Words(RectTransform parent, string text, Vector2 center, Vector2 size, int fontSize, Color color,
                          TextAnchor alignment, bool bold = false)
        {
            var label = NewLabel("Text", parent, text, fontSize, color, alignment);
            label.font = plainFont;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.lineSpacing = 1.05f;
            Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), center, size);
            return label;
        }

        // A panel with a bold heading in its accent color and wrapped body text under it.
        static void Box(RectTransform parent, string heading, string body, Vector2 center, Vector2 size, Color accent)
        {
            Panel(parent, center, size);
            Words(parent, heading, center + new Vector2(0f, size.y * 0.5f - 18f), new Vector2(size.x - 24f, 20f), 14, accent,
                  TextAnchor.MiddleLeft, true);
            Words(parent, body, center + new Vector2(0f, -10f), new Vector2(size.x - 24f, size.y - 36f), 13, Ink, TextAnchor.UpperLeft);
        }

        static void State(RectTransform parent, string name, Vector2 center, Vector2 size, Color accent)
        {
            Panel(parent, center, size);
            Words(parent, name, center, size, 14, accent, TextAnchor.MiddleCenter, true);
        }

        static void Arrow(RectTransform parent, string label, params Vector2[] points) =>
            ArrowLabelAt(parent, label, Vector2.zero, points);

        // An arrow through the given points (straight segments) with a head at the end, and a
        // label centred at labelAt.
        static void ArrowLabelAt(RectTransform parent, string label, Vector2 labelAt, params Vector2[] points)
        {
            var square = EchoSpriteFactory.Load(EchoSpriteFactory.Square);
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector2 a = points[i], b = points[i + 1];
                Vector2 delta = b - a;
                bool last = i == points.Length - 2;
                float length = delta.magnitude - (last ? 8f : 0f);
                var segment = NewImage("Line", parent, square, Line);
                var rect = segment.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = a;
                rect.sizeDelta = new Vector2(Mathf.Max(0f, length) + (last ? 0f : 1.5f), 3f);
                rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }

            Vector2 end = points[points.Length - 1];
            Vector2 direction = (end - points[points.Length - 2]).normalized;
            var head = NewImage("Head", parent, UiSprite("Arrowhead"), Line);
            var headRect = head.rectTransform;
            headRect.anchorMin = headRect.anchorMax = new Vector2(0.5f, 0.5f);
            headRect.pivot = new Vector2(1f, 0.5f);
            headRect.anchoredPosition = end;
            headRect.sizeDelta = new Vector2(13f, 13f);
            headRect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

            if (!string.IsNullOrEmpty(label))
                Words(parent, label, labelAt, new Vector2(150f, 18f), 12, Note, TextAnchor.MiddleCenter);
        }
    }
}
