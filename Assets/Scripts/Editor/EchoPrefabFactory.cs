using Echo.Core;
using Echo.DevTools;
using Echo.Platform;
using Echo.Player;
using Echo.UI;
using Echo.Visuals;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Echo.EditorTools
{
    /// <summary>Builds ECHO's code-generated prefabs: the Player and the persistent Systems.</summary>
    public static class EchoPrefabFactory
    {
        public const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
        public const string PlayerTuningPath = "Assets/Settings/Gameplay/PlayerTuning.asset";
        const string FrictionlessPath = "Assets/Materials/Physics/Frictionless.physicsMaterial2D";

        static readonly Vector2 PlayerColliderSize = new Vector2(0.7f, 1.3f);
        const float PlayerColliderEdgeRadius = 0.05f;
        // Distance from the player's centre down to the bottom of the collider (the feet).
        static float FeetOffset => PlayerColliderSize.y * 0.5f + PlayerColliderEdgeRadius;

        public static PlayerTuning EnsurePlayerTuning()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<PlayerTuning>(PlayerTuningPath);
            if (tuning != null)
                return tuning;

            EchoProjectSetup.EnsureFolderPath("Assets/Settings/Gameplay");
            tuning = ScriptableObject.CreateInstance<PlayerTuning>();
            AssetDatabase.CreateAsset(tuning, PlayerTuningPath);
            return tuning;
        }

        static PhysicsMaterial2D EnsureFrictionless()
        {
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(FrictionlessPath);
            if (material != null)
                return material;

            EchoProjectSetup.EnsureFolderPath("Assets/Materials/Physics");
            // Minimum combine so the player never sticks to walls, whatever they are made of.
            material = new PhysicsMaterial2D("Frictionless")
            {
                friction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine2D.Minimum,
                bounceCombine = PhysicsMaterialCombine2D.Minimum,
            };
            AssetDatabase.CreateAsset(material, FrictionlessPath);
            return material;
        }

        public static GameObject EnsurePlayerPrefab(bool rebuild = false)
        {
            // The graybox player (no PlayerAnimator) is upgraded to the animated one automatically.
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (existing != null && !rebuild && existing.GetComponentInChildren<PlayerAnimator>(true) != null)
                return existing;

            EchoProjectSetup.EnsureFolderPath("Assets/Prefabs/Player");

            var root = new GameObject("Player") { tag = "Player", layer = LayerMask.NameToLayer(Layers.Player) };

            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;

            var collider = root.AddComponent<BoxCollider2D>();
            collider.size = PlayerColliderSize;
            collider.edgeRadius = PlayerColliderEdgeRadius;
            collider.sharedMaterial = EnsureFrictionless();

            root.AddComponent<EnergySystem>();
            root.AddComponent<PlayerMovement>().Tuning = EnsurePlayerTuning();
            root.AddComponent<PlayerDash>();
            var controller = root.AddComponent<PlayerController>();
            root.AddComponent<PlayerFeedback>();

            // Visual: flipped by the controller to face the movement direction.
            //   Squash: scales around the feet (PlayerAnimator) > Body: the animated sprite.
            //   DashTrail: cyan energy trail while dashing (DashEffects also spawns afterimages).
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            SetReference(controller, "visual", visual.transform);

            var squash = new GameObject("Squash");
            squash.transform.SetParent(visual.transform, false);
            squash.transform.localPosition = new Vector3(0f, -FeetOffset, 0f);

            var playerVisuals = EchoArtFactory.EnsurePlayerVisuals();
            var bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(squash.transform, false);
            var bodyRenderer = bodyObject.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = playerVisuals.idle.frames[0];
            bodyRenderer.sortingLayerName = "Player";

            var animator = squash.AddComponent<PlayerAnimator>();
            SetReference(animator, "visuals", playerVisuals);
            SetReference(animator, "body", bodyRenderer);

            var glow = EchoArtFactory.GlowMaterial;
            var trailObject = new GameObject("DashTrail");
            trailObject.transform.SetParent(visual.transform, false);
            trailObject.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            var trail = trailObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = glow;
            trail.time = 0.16f;
            trail.minVertexDistance = 0.08f;
            trail.widthCurve = AnimationCurve.Linear(0f, 0.85f, 1f, 0f);
            trail.colorGradient = Fade(new Color(0.21f, 0.89f, 1f), 0.55f);
            trail.emitting = false;
            trail.sortingLayerName = "Player";
            trail.sortingOrder = -2;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;

            var effects = visual.AddComponent<DashEffects>();
            SetReference(effects, "body", bodyRenderer);
            SetReference(effects, "trail", trail);
            SetReference(effects, "ghostMaterial", glow);

            // A soft personal light keeps the hero readable in the darkest corners.
            var lightObject = new GameObject("Glow");
            lightObject.transform.SetParent(root.transform, false);
            EchoLevelArt.AddPointLight(lightObject, new Color(0.75f, 0.92f, 1f), 0.4f, 2.6f);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        public static void EnsureSystemsPrefab(bool rebuild = false)
        {
            string path = EchoProjectSetup.SystemsPrefabPath;
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            var root = exists && !rebuild ? PrefabUtility.LoadPrefabContents(path) : new GameObject("Systems");

            EnsureComponent<GameManager>(root);
            EnsureComponent<SceneLoader>(root);
            EnsureComponent<GamePixManager>(root);
            EnsureComponent<DevOverlay>(root);

            if (root.transform.Find("UI") == null)
                BuildUI(root.transform);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            if (exists && !rebuild)
                PrefabUtility.UnloadPrefabContents(root);
            else
                Object.DestroyImmediate(root);
        }

        static void BuildUI(Transform systems)
        {
            var ui = new GameObject("UI");
            ui.transform.SetParent(systems, false);

            var eventSystem = new GameObject("EventSystem");
            eventSystem.transform.SetParent(ui.transform, false);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            var canvasObject = new GameObject("GameplayCanvas", typeof(RectTransform));
            canvasObject.transform.SetParent(ui.transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 540f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f; // landscape phones vary in width; keep sizes tied to height
            canvasObject.AddComponent<GraphicRaycaster>();
            var gameplayUI = canvasObject.AddComponent<GameplayUI>();

            // HUD
            var hud = CreateRect("Hud", canvasObject.transform);
            Stretch(hud);
            SetReference(gameplayUI, "hudRoot", hud.gameObject);

            var energy = CreateRect("EnergyHud", hud);
            energy.anchorMin = energy.anchorMax = energy.pivot = new Vector2(0f, 1f);
            energy.anchoredPosition = new Vector2(24f, -20f);
            energy.sizeDelta = new Vector2(300f, 32f);
            var layout = energy.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var energyHud = energy.gameObject.AddComponent<EnergyHud>();

            var pip = CreateRect("PipTemplate", energy);
            pip.sizeDelta = new Vector2(28f, 28f);
            var pipImage = pip.gameObject.AddComponent<Image>();
            pipImage.sprite = EchoSpriteFactory.Load(EchoSpriteFactory.Diamond);
            pipImage.raycastTarget = false;
            pip.gameObject.SetActive(false);
            SetReference(energyHud, "pipTemplate", pipImage);
            SetReference(energyHud, "fullSprite", EchoSpriteFactory.Load(EchoSpriteFactory.Diamond));
            SetReference(energyHud, "emptySprite", EchoSpriteFactory.Load(EchoSpriteFactory.DiamondOutline));

            // Touch controls: each button presses a virtual gamepad control.
            var touch = CreateRect("TouchControls", canvasObject.transform);
            Stretch(touch);
            var touchControls = touch.gameObject.AddComponent<TouchControls>();
            var buttons = CreateRect("Buttons", touch);
            Stretch(buttons);
            SetReference(touchControls, "buttonsRoot", buttons.gameObject);

            var bottomLeft = new Vector2(0f, 0f);
            var bottomRight = new Vector2(1f, 0f);
            var topRight = new Vector2(1f, 1f);
            TouchButton("Left", buttons, bottomLeft, new Vector2(95f, 85f), 130f, "<Gamepad>/dpad/left", EchoSpriteFactory.IconArrow, 180f);
            TouchButton("Right", buttons, bottomLeft, new Vector2(245f, 85f), 130f, "<Gamepad>/dpad/right", EchoSpriteFactory.IconArrow, 0f);
            TouchButton("Jump", buttons, bottomRight, new Vector2(-100f, 100f), 150f, "<Gamepad>/buttonSouth", EchoSpriteFactory.IconJump, 0f);
            TouchButton("Dash", buttons, bottomRight, new Vector2(-260f, 75f), 125f, "<Gamepad>/buttonWest", EchoSpriteFactory.IconDash, 0f);
            TouchButton("Pause", buttons, topRight, new Vector2(-50f, -50f), 70f, "<Gamepad>/start", EchoSpriteFactory.IconPause, 0f);
            buttons.gameObject.SetActive(false);
        }

        static void TouchButton(string name, Transform parent, Vector2 anchor, Vector2 position, float size,
                                string controlPath, string iconPath, float iconRotation)
        {
            var rect = CreateRect(name, parent);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);

            var ring = rect.gameObject.AddComponent<Image>();
            ring.sprite = EchoSpriteFactory.Load(EchoSpriteFactory.TouchRing);
            ring.color = EchoPalette.PlayerBody;
            rect.gameObject.AddComponent<OnScreenButton>().controlPath = controlPath;
            rect.gameObject.AddComponent<TouchButtonVisual>();

            var icon = CreateRect("Icon", rect);
            Stretch(icon);
            icon.localEulerAngles = new Vector3(0f, 0f, iconRotation);
            var iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.sprite = EchoSpriteFactory.Load(iconPath);
            iconImage.color = EchoPalette.PlayerBody;
            iconImage.raycastTarget = false;
        }

        static Gradient Fade(Color color, float alpha)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(alpha, 0f), new GradientAlphaKey(0f, 1f) });
            return gradient;
        }

        static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        public static SpriteRenderer AddSprite(GameObject target, string spritePath, Color color, string sortingLayer, int order)
        {
            var renderer = target.AddComponent<SpriteRenderer>();
            renderer.sprite = EchoSpriteFactory.Load(spritePath);
            renderer.color = color;
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = order;
            return renderer;
        }

        public static void SetReference(Object component, string field, Object value)
        {
            var serialized = new SerializedObject(component);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureComponent<T>(GameObject target) where T : Component
        {
            if (target.GetComponent<T>() == null)
                target.AddComponent<T>();
        }
    }
}
