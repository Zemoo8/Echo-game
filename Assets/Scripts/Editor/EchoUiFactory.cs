using Echo.Audio;
using Echo.Core;
using Echo.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Echo.EditorTools
{
    /// <summary>
    /// Builds ECHO's interface in code: the persistent UI on the Systems prefab (energy meter,
    /// level title and level-complete cards, death flash, pause menu, touch controls, screen fader)
    /// and the main menu's canvas. The UI is rebuilt from scratch each time, so nothing in it
    /// should be edited by hand.
    /// </summary>
    public static class EchoUiFactory
    {
        const string UiArt = "Assets/Art/Pixel/UI";
        const string FontPath = "Assets/ThirdParty/Kenney/Fonts/KenneyFuture.ttf";
        internal const string GlowPath = "Assets/ThirdParty/Kenney/Particles/light_01.png";

        // Pixel art is 32 px per unit; at 96 reference px per unit every art pixel is 3 UI units,
        // so at 960x540 the UI shares the world's 3x pixel scale.
        const float ReferencePixelsPerUnit = 96f;
        const float Px = 3f;

        internal static readonly Color Dim = new Color(0.47f, 0.63f, 0.71f);
        static readonly Color TextShadow = new Color(0f, 0.016f, 0.063f, 0.85f);
        static readonly Color Clear = new Color(1f, 1f, 1f, 0f);

        static Font font;

        static Font UiFont
        {
            get
            {
                if (font == null)
                    font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
                if (font == null)
                    throw new System.InvalidOperationException($"[ECHO UI] Missing font {FontPath}. Run Tools/import_assets.py.");
                return font;
            }
        }

        /// <summary>Replaces the UI on the Systems prefab with a freshly built one.</summary>
        public static void RebuildSystemsUI()
        {
            string path = EchoProjectSetup.SystemsPrefabPath;
            var root = PrefabUtility.LoadPrefabContents(path);
            var old = root.transform.Find("UI");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
            BuildSystemsUI(root.transform);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        // --- persistent gameplay UI -------------------------------------------------------------

        public static void BuildSystemsUI(Transform systems)
        {
            var ui = new GameObject("UI");
            ui.transform.SetParent(systems, false);

            var eventSystem = new GameObject("EventSystem");
            eventSystem.transform.SetParent(ui.transform, false);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            // Back to front: death flash, HUD, cards, touch buttons, pause menu.
            var canvas = CreateCanvas("GameplayCanvas", ui.transform, 10);
            var gameplayUI = canvas.gameObject.AddComponent<GameplayUI>();
            BuildDamageFlash(canvas);
            EchoPrefabFactory.SetReference(gameplayUI, "hudRoot", BuildHud(canvas).gameObject);
            BuildIntroCard(canvas);
            BuildCompleteCard(canvas);
            BuildTouchControls(canvas);
            BuildPauseMenu(canvas);

            var fadeCanvas = CreateCanvas("FadeCanvas", ui.transform, 100);
            var cover = NewImage("Cover", fadeCanvas, EchoSpriteFactory.Load(EchoSpriteFactory.Square), EchoPalette.Void);
            Stretch(cover.rectTransform);
            var fader = fadeCanvas.gameObject.AddComponent<ScreenFader>();
            EchoPrefabFactory.SetReference(fader, "cover", cover);
        }

        static RectTransform BuildHud(RectTransform canvas)
        {
            var hud = NewRect("Hud", canvas);
            Stretch(hud);

            // Panel that sizes itself around the bolt icon and the cells.
            var meter = NewRect("EnergyMeter", hud);
            Place(meter, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -16f), Vector2.zero);
            var panel = meter.gameObject.AddComponent<Image>();
            panel.sprite = UiSprite("Panel");
            panel.type = UnityEngine.UI.Image.Type.Sliced;
            panel.raycastTarget = false;
            var layout = meter.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset((int)(5 * Px), (int)(5 * Px), (int)(4 * Px), (int)(4 * Px));
            layout.spacing = 2 * Px;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var fitter = meter.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var icon = NewImage("Bolt", meter, UiSprite("Bolt"), EchoPalette.Energy);
            icon.rectTransform.sizeDelta = PixelSize(icon.sprite);

            var cell = NewRect("Cell", meter);
            cell.sizeDelta = PixelSize(UiSprite("CellEmpty"));
            var glow = NewImage("Glow", cell, AssetDatabase.LoadAssetAtPath<Sprite>(GlowPath), Clear);
            glow.rectTransform.sizeDelta = new Vector2(78f, 78f);
            var frame = NewImage("Frame", cell, UiSprite("CellEmpty"), Color.white);
            Stretch(frame.rectTransform);
            var fill = NewImage("Fill", cell, UiSprite("CellFull"), Clear);
            Stretch(fill.rectTransform);
            var cellView = cell.gameObject.AddComponent<EnergyCellView>();
            EchoPrefabFactory.SetReference(cellView, "fill", fill);
            EchoPrefabFactory.SetReference(cellView, "glow", glow);
            cell.gameObject.SetActive(false);

            var energyHud = meter.gameObject.AddComponent<EnergyHud>();
            EchoPrefabFactory.SetReference(energyHud, "cellTemplate", cellView);
            EchoPrefabFactory.SetReference(energyHud, "icon", icon);
            EchoPrefabFactory.SetReference(energyHud, "shakeTarget", meter);
            return hud;
        }

        static void BuildIntroCard(RectTransform canvas)
        {
            var card = NewRect("LevelIntro", canvas);
            Place(card, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(640f, 90f));
            var group = Group(card);

            var sector = NewLabel("Sector", card, "SECTOR 00", 14, EchoPalette.Energy, TextAnchor.MiddleCenter);
            Place(sector.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(640f, 20f));
            var title = NewLabel("Title", card, "LEVEL", 30, Color.white, TextAnchor.MiddleCenter);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(640f, 42f));
            var line = NewImage("Underline", card, EchoSpriteFactory.Load(EchoSpriteFactory.Square), WithAlpha(EchoPalette.Energy, 0.7f));
            Place(line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(260f, 3f));

            var intro = card.gameObject.AddComponent<LevelIntroCard>();
            EchoPrefabFactory.SetReference(intro, "group", group);
            EchoPrefabFactory.SetReference(intro, "sectorText", sector);
            EchoPrefabFactory.SetReference(intro, "titleText", title);
            EchoPrefabFactory.SetReference(intro, "underline", line.rectTransform);
        }

        static void BuildCompleteCard(RectTransform canvas)
        {
            var holder = NewRect("LevelComplete", canvas);
            Stretch(holder);
            var card = NewRect("Card", holder);
            Stretch(card);
            var group = Group(card);

            var dim = NewImage("Dim", card, EchoSpriteFactory.Load(EchoSpriteFactory.Square), WithAlpha(EchoPalette.Void, 0.45f));
            Stretch(dim.rectTransform);

            var panel = NewPanel("Panel", card, new Vector2(330f, 234f));
            var header = NewLabel("Header", panel, "SECTOR CLEARED", 26, EchoPalette.Exit, TextAnchor.MiddleCenter);
            Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(330f, 36f));
            var divider = NewImage("Divider", panel, EchoSpriteFactory.Load(EchoSpriteFactory.Square), WithAlpha(EchoPalette.Exit, 0.5f));
            Place(divider.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(250f, 3f));

            var time = StatRow(panel, "TIME", -82f);
            var dashes = StatRow(panel, "DASHES", -116f);
            var resets = StatRow(panel, "RESETS", -150f);

            var track = NewImage("Progress", panel, EchoSpriteFactory.Load(EchoSpriteFactory.Square), new Color(1f, 1f, 1f, 0.12f));
            Place(track.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(250f, 4f));
            var fill = NewImage("Fill", track.rectTransform, EchoSpriteFactory.Load(EchoSpriteFactory.Square), EchoPalette.Exit);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;

            var hintText = NewLabel("Hint", panel, "PRESS JUMP TO CONTINUE", 11, Dim, TextAnchor.MiddleCenter);
            Place(hintText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(300f, 18f));
            var hint = hintText.gameObject.AddComponent<CanvasGroup>();

            var complete = holder.gameObject.AddComponent<LevelCompleteCard>();
            EchoPrefabFactory.SetReference(complete, "group", group);
            EchoPrefabFactory.SetReference(complete, "panel", panel);
            EchoPrefabFactory.SetReference(complete, "timeValue", time);
            EchoPrefabFactory.SetReference(complete, "dashesValue", dashes);
            EchoPrefabFactory.SetReference(complete, "resetsValue", resets);
            EchoPrefabFactory.SetReference(complete, "progressFill", fill.rectTransform);
            EchoPrefabFactory.SetReference(complete, "hint", hint);
            card.gameObject.SetActive(false);
        }

        static Text StatRow(RectTransform panel, string name, float y)
        {
            var label = NewLabel(name, panel, name, 14, Dim, TextAnchor.MiddleLeft);
            Place(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(-125f, y), new Vector2(130f, 28f));
            var value = NewLabel(name + "Value", panel, "0", 20, Color.white, TextAnchor.MiddleRight);
            Place(value.rectTransform, new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(125f, y), new Vector2(150f, 28f));
            return value;
        }

        static void BuildDamageFlash(RectTransform canvas)
        {
            var vignette = NewImage("DamageFlash", canvas, EchoSpriteFactory.Load(EchoSpriteFactory.Vignette), WithAlpha(EchoPalette.Hazard, 0f));
            Stretch(vignette.rectTransform);
            var flash = vignette.gameObject.AddComponent<DamageFlash>();
            EchoPrefabFactory.SetReference(flash, "vignette", vignette);
        }

        static void BuildPauseMenu(RectTransform canvas)
        {
            var menu = NewRect("PauseMenu", canvas);
            Stretch(menu);
            var root = NewRect("Root", menu);
            Stretch(root);
            var group = Group(root);

            // The dim layer catches clicks and touches so nothing behind the menu reacts.
            var dim = NewImage("Dim", root, EchoSpriteFactory.Load(EchoSpriteFactory.Square), WithAlpha(EchoPalette.Void, 0.72f));
            dim.raycastTarget = true;
            Stretch(dim.rectTransform);

            var panel = NewPanel("Panel", root, new Vector2(300f, 318f));
            var title = NewLabel("Title", panel, "PAUSED", 28, Color.white, TextAnchor.MiddleCenter);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(300f, 40f));
            var divider = NewImage("Divider", panel, EchoSpriteFactory.Load(EchoSpriteFactory.Square), WithAlpha(EchoPalette.Energy, 0.5f));
            Place(divider.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(220f, 3f));

            var list = NewRect("List", panel);
            Place(list, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(252f, 200f));
            string[] names = { "RESUME", "RESTART", "MUSIC: ON", "EFFECTS: ON", "MAIN MENU" };
            var entries = new MenuButton[names.Length];
            for (int i = 0; i < names.Length; i++)
                entries[i] = MenuEntry(names[i], list, names[i], new Vector2(0.5f, 1f), new Vector2(0f, -i * 40f), new Vector2(252f, 34f), 18, true);
            SetItems(list.gameObject.AddComponent<MenuList>(), entries);

            var hint = NewLabel("Hint", panel, "ESC / P TO RESUME", 11, Dim, TextAnchor.MiddleCenter);
            Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(300f, 18f));

            var pause = menu.gameObject.AddComponent<PauseMenu>();
            EchoPrefabFactory.SetReference(pause, "root", root.gameObject);
            EchoPrefabFactory.SetReference(pause, "group", group);
            EchoPrefabFactory.SetReference(pause, "panel", panel);
            EchoPrefabFactory.SetReference(pause, "resume", entries[0]);
            EchoPrefabFactory.SetReference(pause, "restart", entries[1]);
            EchoPrefabFactory.SetReference(pause, "music", entries[2]);
            EchoPrefabFactory.SetReference(pause, "sound", entries[3]);
            EchoPrefabFactory.SetReference(pause, "mainMenu", entries[4]);
            root.gameObject.SetActive(false);
        }

        // Touch controls: each button presses a virtual gamepad control.
        static void BuildTouchControls(RectTransform canvas)
        {
            var touch = NewRect("TouchControls", canvas);
            Stretch(touch);
            var touchControls = touch.gameObject.AddComponent<TouchControls>();
            var buttons = NewRect("Buttons", touch);
            Stretch(buttons);
            EchoPrefabFactory.SetReference(touchControls, "buttonsRoot", buttons.gameObject);

            var bottomLeft = new Vector2(0f, 0f);
            var bottomRight = new Vector2(1f, 0f);
            var topRight = new Vector2(1f, 1f);
            Color white = EchoPalette.PlayerBody;
            TouchButton("Left", buttons, bottomLeft, new Vector2(95f, 85f), 130f, "<Gamepad>/dpad/left", EchoSpriteFactory.IconArrow, 180f, white);
            TouchButton("Right", buttons, bottomLeft, new Vector2(245f, 85f), 130f, "<Gamepad>/dpad/right", EchoSpriteFactory.IconArrow, 0f, white);
            TouchButton("Jump", buttons, bottomRight, new Vector2(-100f, 100f), 150f, "<Gamepad>/buttonSouth", EchoSpriteFactory.IconJump, 0f, white);
            // Dashing spends energy, so the dash button wears the energy color.
            TouchButton("Dash", buttons, bottomRight, new Vector2(-260f, 75f), 125f, "<Gamepad>/buttonWest", EchoSpriteFactory.IconDash, 0f, EchoPalette.Energy);
            TouchButton("Pause", buttons, topRight, new Vector2(-50f, -50f), 70f, "<Gamepad>/start", EchoSpriteFactory.IconPause, 0f, white);
            buttons.gameObject.SetActive(false);
        }

        static void TouchButton(string name, Transform parent, Vector2 anchor, Vector2 position, float size,
                                string controlPath, string iconPath, float iconRotation, Color color)
        {
            var rect = NewRect(name, parent);
            Place(rect, anchor, new Vector2(0.5f, 0.5f), position, new Vector2(size, size));

            var ring = rect.gameObject.AddComponent<Image>();
            ring.sprite = EchoSpriteFactory.Load(EchoSpriteFactory.TouchRing);
            ring.color = color;
            rect.gameObject.AddComponent<OnScreenButton>().controlPath = controlPath;
            rect.gameObject.AddComponent<TouchButtonVisual>();

            var icon = NewImage("Icon", rect, EchoSpriteFactory.Load(iconPath), color);
            Stretch(icon.rectTransform);
            icon.rectTransform.localEulerAngles = new Vector3(0f, 0f, iconRotation);
        }

        // --- main menu --------------------------------------------------------------------------

        /// <summary>The title screen's canvas, added to the open (main menu) scene.</summary>
        public static void BuildMainMenuUI(string firstScene)
        {
            var canvas = CreateCanvas("MenuCanvas", null, 0);

            // Darkens the left of the screen so the logo and menu read over the lab.
            var shade = NewImage("Shade", canvas, EchoSpriteFactory.Load(EchoSpriteFactory.ShadeLeft), WithAlpha(EchoPalette.Void, 0.9f));
            shade.rectTransform.anchorMin = Vector2.zero;
            shade.rectTransform.anchorMax = new Vector2(0f, 1f);
            shade.rectTransform.pivot = new Vector2(0f, 0.5f);
            shade.rectTransform.sizeDelta = new Vector2(640f, 0f);

            var content = NewRect("Content", canvas);
            Stretch(content);
            var contentGroup = content.gameObject.AddComponent<CanvasGroup>();

            var logoSprite = UiSprite("Logo");
            Vector2 logoSize = PixelSize(logoSprite);
            Vector2 logoPosition = new Vector2(62f, -70f);

            var glowRect = NewRect("LogoGlow", content);
            Place(glowRect, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), logoPosition + new Vector2(logoSize.x * 0.5f, -logoSize.y * 0.5f), new Vector2(logoSize.x * 1.5f, logoSize.y * 2.1f));
            var glowGroup = glowRect.gameObject.AddComponent<CanvasGroup>();
            glowGroup.blocksRaycasts = false;
            var glow = NewImage("Glow", glowRect, AssetDatabase.LoadAssetAtPath<Sprite>(GlowPath), WithAlpha(EchoPalette.Energy, 0.2f));
            Stretch(glow.rectTransform);

            var logo = NewImage("Logo", content, logoSprite, Color.white);
            Place(logo.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), logoPosition, logoSize);

            var tagline = NewLabel("Tagline", content, "EVERY DASH IS A DECISION", 14, EchoPalette.Energy, TextAnchor.MiddleLeft);
            Place(tagline.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(logoPosition.x + 6f, logoPosition.y - logoSize.y - 12f), new Vector2(420f, 22f));

            // Main list.
            var mainPanel = NewRect("MainPanel", content);
            Place(mainPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(56f, -250f), new Vector2(270f, 168f));
            string[] names = { "PLAY", "MUSIC: ON", "EFFECTS: ON", "CREDITS" };
            var entries = new MenuButton[names.Length];
            for (int i = 0; i < names.Length; i++)
                entries[i] = MenuEntry(names[i], mainPanel, names[i], new Vector2(0f, 1f), new Vector2(0f, -i * 42f), new Vector2(270f, 36f), 20, false);
            SetItems(mainPanel.gameObject.AddComponent<MenuList>(), entries);

            // Credits. Headings use Kenney Future; names use Unity's built-in font, because
            // Kenney Future's K is nearly identical to its H ("Kenney" would read "Henney").
            var credits = NewPanel("CreditsPanel", content, Vector2.zero);
            Place(credits, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(56f, -218f), new Vector2(450f, 262f));
            var creditsTitle = NewLabel("Title", credits, "CREDITS", 20, Color.white, TextAnchor.MiddleLeft);
            Place(creditsTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -14f), new Vector2(300f, 30f));
            var plainFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            (string heading, string line)[] rows =
            {
                ("ART", "Luis Zuno (ansimuz): Warped City, Sideview Sci-Fi"),
                ("FONT, SOUND EFFECTS, PARTICLES", "Kenney (kenney.nl)"),
                ("MUSIC", "SRG774: Dark Sci-Fi Audio Pack"),
                ("LICENSES", "All CC0 (public domain). Full list in ASSET_LICENSES.md"),
            };
            for (int i = 0; i < rows.Length; i++)
            {
                float y = -50f - i * 40f;
                var heading = NewLabel("Heading", credits, rows[i].heading, 11, EchoPalette.Energy, TextAnchor.UpperLeft);
                Place(heading.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, y), new Vector2(400f, 16f));
                var line = NewLabel("Line", credits, rows[i].line, 14, new Color(0.85f, 0.92f, 0.96f), TextAnchor.UpperLeft);
                line.font = plainFont;
                Place(line.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, y - 15f), new Vector2(400f, 20f));
            }
            var backList = NewRect("List", credits);
            Place(backList, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(14f, 10f), new Vector2(170f, 36f));
            // "RETURN", not "BACK": in Kenney Future K (and X) look like H, so UI words avoid them.
            var back = MenuEntry("RETURN", backList, "RETURN", new Vector2(0f, 1f), Vector2.zero, new Vector2(170f, 36f), 18, false);
            var backSettings = new SerializedObject(back);
            backSettings.FindProperty("clickSound").enumValueIndex = (int)SfxId.UiBack;
            backSettings.ApplyModifiedPropertiesWithoutUndo();
            SetItems(backList.gameObject.AddComponent<MenuList>(), new[] { back });
            credits.gameObject.SetActive(false);

            var controls = NewLabel("Controls", content, "MOVE  A / D     JUMP  SPACE     DASH  SHIFT     PAUSE  ESC", 11, Dim, TextAnchor.MiddleLeft);
            Place(controls.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(64f, 22f), new Vector2(700f, 18f));

            var menu = canvas.gameObject.AddComponent<MainMenu>();
            var settings = new SerializedObject(menu);
            settings.FindProperty("firstScene").stringValue = firstScene;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EchoPrefabFactory.SetReference(menu, "content", contentGroup);
            EchoPrefabFactory.SetReference(menu, "logo", logo.rectTransform);
            EchoPrefabFactory.SetReference(menu, "logoGlow", glowGroup);
            EchoPrefabFactory.SetReference(menu, "mainPanel", mainPanel.gameObject);
            EchoPrefabFactory.SetReference(menu, "creditsPanel", credits.gameObject);
            EchoPrefabFactory.SetReference(menu, "play", entries[0]);
            EchoPrefabFactory.SetReference(menu, "music", entries[1]);
            EchoPrefabFactory.SetReference(menu, "sound", entries[2]);
            EchoPrefabFactory.SetReference(menu, "credits", entries[3]);
            EchoPrefabFactory.SetReference(menu, "creditsBack", back);
        }

        // --- building blocks --------------------------------------------------------------------

        /// <summary>
        /// A menu entry: invisible hit area, selection bar, chevron and label. Centred entries
        /// (pause menu) keep the label still; left-aligned ones (main menu) nudge it right.
        /// </summary>
        internal static MenuButton MenuEntry(string name, RectTransform parent, string text, Vector2 anchor, Vector2 position,
                                    Vector2 size, int fontSize, bool centred)
        {
            var rect = NewRect(name, parent);
            Place(rect, anchor, new Vector2(anchor.x, 1f), position, size);

            var hitArea = rect.gameObject.AddComponent<Image>();
            hitArea.sprite = EchoSpriteFactory.Load(EchoSpriteFactory.Square);
            hitArea.color = Clear;

            var highlight = NewImage("Highlight", rect, UiSprite("SelectBar"), Clear);
            highlight.type = UnityEngine.UI.Image.Type.Sliced;
            Stretch(highlight.rectTransform);

            var chevron = NewImage("Chevron", rect, UiSprite("Chevron"), Clear);
            Place(chevron.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), PixelSize(chevron.sprite));

            var label = NewLabel("Label", rect, text, fontSize, Dim, centred ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft);
            Stretch(label.rectTransform);
            if (!centred)
                label.rectTransform.offsetMin = new Vector2(38f, 0f);

            var button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = hitArea;

            var entry = rect.gameObject.AddComponent<MenuButton>();
            EchoPrefabFactory.SetReference(entry, "label", label);
            EchoPrefabFactory.SetReference(entry, "highlight", highlight);
            EchoPrefabFactory.SetReference(entry, "chevron", chevron);
            var settings = new SerializedObject(entry);
            settings.FindProperty("idleColor").colorValue = Dim;
            settings.FindProperty("labelShift").floatValue = centred ? 0f : 6f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            return entry;
        }

        internal static void SetItems(MenuList list, MenuButton[] items)
        {
            var settings = new SerializedObject(list);
            var array = settings.FindProperty("items");
            array.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>A centred, sliced ECHO panel.</summary>
        internal static RectTransform NewPanel(string name, RectTransform parent, Vector2 size)
        {
            var panel = NewImage(name, parent, UiSprite("Panel"), Color.white);
            panel.type = UnityEngine.UI.Image.Type.Sliced;
            Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            return panel.rectTransform;
        }

        internal static RectTransform CreateCanvas(string name, Transform parent, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null)
                go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 540f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f; // landscape phones vary in width; keep sizes tied to height
            scaler.referencePixelsPerUnit = ReferencePixelsPerUnit;
            go.AddComponent<GraphicRaycaster>();
            return (RectTransform)go.transform;
        }

        internal static CanvasGroup Group(RectTransform target)
        {
            var group = target.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            return group;
        }

        internal static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        internal static Image NewImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var image = NewRect(name, parent).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        internal static Text NewLabel(string name, Transform parent, string text, int size, Color color, TextAnchor alignment)
        {
            var label = NewRect(name, parent).gameObject.AddComponent<Text>();
            label.font = UiFont;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.supportRichText = false;
            label.raycastTarget = false;
            var shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = TextShadow;
            shadow.effectDistance = new Vector2(2f, -2f);
            return label;
        }

        internal static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        internal static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        internal static Sprite UiSprite(string name) => EchoArtFactory.LoadSprite($"{UiArt}/{name}.png");

        internal static Vector2 PixelSize(Sprite sprite) => sprite.rect.size * Px;

        internal static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
