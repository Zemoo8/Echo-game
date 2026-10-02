using Echo.Visuals;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Echo.EditorTools
{
    /// <summary>
    /// Dresses code-built levels with ECHO's art: panel blocks, backdrops, parallax, props, pit
    /// warnings, energy cells, energy barriers, lights and ambient dust. The lab helpers are the
    /// original biome 1 set; the kit helpers dress any biome from its EnvironmentKit.
    /// </summary>
    public static class EchoLevelArt
    {
        /// <summary>The lab corridor's darkest tone; the camera clears to it so the art has no edges.</summary>
        public static readonly Color LabVoid = new Color32(0x00, 0x04, 0x10, 0xFF);

        const string Lab = "Assets/ThirdParty/Ansimuz/SideviewSciFi/Lab";
        const string Core = "Assets/ThirdParty/Ansimuz/SideviewSciFi/Core";
        const string Security = "Assets/ThirdParty/Ansimuz/SideviewSciFi/Security";
        const string Props = "Assets/ThirdParty/Ansimuz/WarpedCity/Props";
        const string Pixel = "Assets/Art/Pixel";
        const string Particles = "Assets/ThirdParty/Kenney/Particles";

        static readonly Color EnergyLight = new Color(0.4f, 0.9f, 1f);
        static readonly Color ExitLight = new Color(0.35f, 1f, 0.6f);
        static readonly Color DangerLight = new Color(1f, 0.3f, 0.25f);
        static readonly Color LampLight = new Color(0.55f, 0.85f, 0.95f);
        static readonly Color ScannerRed = new Color(1f, 0.25f, 0.2f);

        // --- solid level geometry ---------------------------------------------------------------

        /// <summary>Lab panels drawn at the block's size (corners fixed, panels tiled).</summary>
        public static SpriteRenderer DressBlock(GameObject block, Vector2 size)
        {
            var renderer = block.AddComponent<SpriteRenderer>();
            renderer.sprite = EchoArtFactory.LoadSprite($"{Pixel}/Lab/LabBlock.png");
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = size;
            renderer.sortingLayerName = "Level";
            return renderer;
        }

        /// <summary>The kit's ground panels drawn at the block's size.</summary>
        public static SpriteRenderer DressBlock(GameObject block, Vector2 size, EnvironmentKit kit)
        {
            var renderer = DressBlock(block, size);
            renderer.sprite = kit.block;
            return renderer;
        }

        /// <summary>Darkens solid ground below the walkable surface so only the lit edge stands out.</summary>
        public static void DepthShade(Transform parent, float xMin, float xMax, float top)
        {
            var go = new GameObject("DepthShade");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3((xMin + xMax) * 0.5f, top, 0f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = EchoArtFactory.LoadSprite($"{Pixel}/Lab/DepthShade.png");
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(xMax - xMin, 4f);
            // The sprite is centred; shift so its top edge sits at 'top'.
            go.transform.position += Vector3.down * 2f;
            renderer.sortingLayerName = "Level";
            renderer.sortingOrder = 5;
        }

        // --- backdrop ---------------------------------------------------------------------------

        /// <summary>
        /// Lines the level with lab corridor segments from <paramref name="xMin"/> up to the exit
        /// segment centred on <paramref name="exitX"/>, bottoms at <paramref name="floorY"/>.
        /// </summary>
        public static void Corridor(Transform parent, float xMin, float exitX, float floorY)
        {
            var root = new GameObject("Corridor").transform;
            root.SetParent(parent, false);

            string[] sequence =
            {
                "Corridor_Shutter", "Corridor_A", "Corridor_Machinery", "Corridor_Terminal", "Corridor_A",
                "Corridor_RoomEnd", "Corridor_Machinery", "Corridor_A", "Corridor_Terminal", "Corridor_A",
                "Corridor_Machinery", "Corridor_A", "Corridor_Shutter",
            };

            var exit = EchoArtFactory.LoadSprite($"{Lab}/Corridor_Exit.png");
            float exitStart = exitX - exit.bounds.size.x * 0.5f;

            float x = xMin;
            int index = 0;
            while (x < exitStart)
            {
                var sprite = EchoArtFactory.LoadSprite($"{Lab}/{sequence[index % sequence.Length]}.png");
                float width = sprite.bounds.size.x;
                PlaceBottom(root, sprite.name, sprite, new Vector2(x + width * 0.5f, floorY), "Background", 1);
                x += width;
                index++;
            }
            // The exit segment draws over any piece that runs under it.
            PlaceBottom(root, "Corridor_Exit", exit, new Vector2(exitX, floorY), "Background", 2);
        }

        /// <summary>Lab corridor segments placed left to right from <paramref name="xMin"/>, in the order given.</summary>
        public static void CorridorPieces(Transform parent, float xMin, float floorY, params string[] pieces)
        {
            var root = new GameObject("Corridor").transform;
            root.SetParent(parent, false);

            float x = xMin;
            foreach (string piece in pieces)
            {
                var sprite = EchoArtFactory.LoadSprite($"{Lab}/{piece}.png");
                float width = sprite.bounds.size.x;
                PlaceBottom(root, piece, sprite, new Vector2(x + width * 0.5f, floorY), "Background", 1);
                x += width;
            }
        }

        /// <summary>
        /// The city seen through the corridor's windows: a slow parallax strip behind everything.
        /// </summary>
        public static void FarCity(Transform parent, float xMin, float xMax, float floorY, bool parallax = true)
        {
            var go = new GameObject("FarCity");
            go.transform.SetParent(parent, false);
            var sprite = EchoArtFactory.LoadSprite($"{Lab}/Corridor_Back.png");
            float height = sprite.bounds.size.y;
            float pivotY = sprite.pivot.y / sprite.rect.height; // tiled sprites keep their pivot
            go.transform.position = new Vector3((xMin + xMax) * 0.5f, floorY + height * pivotY, 0f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(xMax - xMin + 20f, height);
            renderer.color = new Color(0.6f, 0.8f, 0.95f); // cooler and dimmer than the lab itself
            renderer.sortingLayerName = "Background";
            renderer.sortingOrder = 0;

            if (parallax)
                SetParallax(go.AddComponent<ParallaxLayer>(), new Vector2(0.85f, 0.1f));
        }

        /// <summary>
        /// Dim machinery in the space above the corridor ceiling, fading up into the void, so the
        /// top of the screen reads as deep structure instead of an empty band.
        /// </summary>
        public static void UpperMachinery(Transform parent, float xMin, float xMax, float bottomY)
        {
            var sprite = EchoArtFactory.LoadSprite($"{Core}/Machinery_Back.png");
            var go = new GameObject("UpperMachinery");
            go.transform.SetParent(parent, false);
            float pivotY = sprite.pivot.y / sprite.rect.height;
            go.transform.position = new Vector3((xMin + xMax) * 0.5f, bottomY + sprite.bounds.size.y * pivotY, 0f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(xMax - xMin, sprite.bounds.size.y);
            renderer.color = new Color(0.32f, 0.48f, 0.55f); // pushed back: darker and greener than the corridor
            renderer.sortingLayerName = "Background";
            renderer.sortingOrder = 1;

            // The depth shade, flipped: clear at the bottom, void at the top.
            var fade = new GameObject("UpperFade");
            fade.transform.SetParent(parent, false);
            fade.transform.position = new Vector3((xMin + xMax) * 0.5f, bottomY + 0.75f + 2f, 0f);
            fade.transform.localScale = new Vector3(1f, -1f, 1f);
            var fadeRenderer = fade.AddComponent<SpriteRenderer>();
            fadeRenderer.sprite = EchoArtFactory.LoadSprite($"{Pixel}/Lab/DepthShade.png");
            fadeRenderer.drawMode = SpriteDrawMode.Tiled;
            fadeRenderer.size = new Vector2(xMax - xMin, 4f);
            fadeRenderer.sortingLayerName = "Background";
            fadeRenderer.sortingOrder = 3;
        }

        // --- kit backdrops (any biome) ------------------------------------------------------------

        /// <summary>The kit's wall pieces laid left to right from xMin to xMax, bottoms at floorY.</summary>
        public static void WallStrip(EnvironmentKit kit, Transform parent, float xMin, float xMax, float floorY, int order = 1)
        {
            var root = new GameObject("Walls").transform;
            root.SetParent(parent, false);
            float x = xMin;
            for (int i = 0; x < xMax; i++)
            {
                var sprite = kit.backdrop[i % kit.backdrop.Length];
                float width = sprite.bounds.size.x;
                var piece = PlaceBottom(root, sprite.name, sprite, new Vector2(x + width * 0.5f, floorY), "Background", order);
                piece.GetComponent<SpriteRenderer>().color = kit.backdropTint;
                x += width;
            }
        }

        /// <summary>
        /// Security wing: steel bulkheads, cables and a robot arm hanging in front, and red
        /// scanners along the ceiling.
        /// </summary>
        public static void SecurityHall(EnvironmentKit kit, Transform parent, float xMin, float xMax, float floorY)
        {
            WallStrip(kit, parent, xMin, xMax, floorY);
            float wallTop = floorY + kit.backdrop[0].bounds.size.y;

            var cables = AssetDatabase.LoadAssetAtPath<Sprite>($"{Security}/Bulkhead_foreground.png");
            if (cables != null)
            {
                var go = new GameObject("HangingCables");
                go.transform.SetParent(parent, false);
                float height = cables.bounds.size.y;
                float pivotY = cables.pivot.y / cables.rect.height;
                go.transform.position = new Vector3((xMin + xMax) * 0.5f, wallTop - height + height * pivotY, 0f);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = cables;
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.size = new Vector2(xMax - xMin, height);
                renderer.color = new Color(0.55f, 0.6f, 0.68f); // between the wall and the level
                renderer.sortingLayerName = "Midground";
                renderer.sortingOrder = -5;
            }

            for (float x = xMin + 4f; x < xMax - 2f; x += 9f)
                Scanner(parent, x, wallTop - 0.4f, floorY);
        }

        /// <summary>A ceiling scanner: red lens, a faint beam and a red light that pulses.</summary>
        public static void Scanner(Transform parent, float x, float y, float floorY)
        {
            var go = new GameObject("Scanner");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            var lamp = go.AddComponent<SpriteRenderer>();
            lamp.sprite = EchoArtFactory.LoadSprite($"{Pixel}/Security/ScannerLamp.png");
            lamp.sortingLayerName = "Midground";

            // A soft glow stretched down from the lens: light, not a solid laser.
            var beamSprite = EchoArtFactory.LoadSprite($"{Particles}/light_01.png");
            var beam = new GameObject("Beam");
            beam.transform.SetParent(go.transform, false);
            float length = Mathf.Max(1f, y - floorY);
            beam.transform.localPosition = new Vector3(0f, -length * 0.45f, 0f);
            beam.transform.localScale = new Vector3(1.6f / beamSprite.bounds.size.x, length * 1.1f / beamSprite.bounds.size.y, 1f);
            var beamRenderer = beam.AddComponent<SpriteRenderer>();
            beamRenderer.sprite = beamSprite;
            beamRenderer.sharedMaterial = EchoArtFactory.SoftGlowMaterial;
            beamRenderer.color = new Color(1f, 0.25f, 0.2f, 0.22f);
            beamRenderer.sortingLayerName = "Midground";
            beamRenderer.sortingOrder = -1;

            var lightObject = new GameObject("ScanLight");
            lightObject.transform.SetParent(go.transform, false);
            lightObject.transform.localPosition = new Vector3(0f, -1.2f, 0f);
            AddPointLight(lightObject, ScannerRed, 0.9f, 2.6f);
            var flicker = lightObject.AddComponent<LightFlicker>();
            var settings = new SerializedObject(flicker);
            settings.FindProperty("pulse").floatValue = 0.6f;
            settings.FindProperty("pulseSpeed").floatValue = 1.6f;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Energy core: two rows of machinery wall, cyan conduits between, fading up into the dark.</summary>
        public static void CoreMachinery(EnvironmentKit kit, Transform parent, float xMin, float xMax, float floorY)
        {
            float rowHeight = kit.backdrop[0].bounds.size.y;
            WallStrip(kit, parent, xMin, xMax, floorY);
            WallStrip(kit, parent, xMin, xMax, floorY + rowHeight);

            for (float x = xMin + 2.5f; x < xMax; x += 6.5f)
                Conduit(parent, x, floorY, rowHeight * 2f);

            var fade = new GameObject("UpperFade");
            fade.transform.SetParent(parent, false);
            fade.transform.position = new Vector3((xMin + xMax) * 0.5f, floorY + rowHeight * 2f - 1.25f + 2f, 0f);
            fade.transform.localScale = new Vector3(1f, -1f, 1f);
            var fadeRenderer = fade.AddComponent<SpriteRenderer>();
            fadeRenderer.sprite = EchoArtFactory.LoadSprite($"{Pixel}/Lab/DepthShade.png");
            fadeRenderer.drawMode = SpriteDrawMode.Tiled;
            fadeRenderer.size = new Vector2(xMax - xMin, 4f);
            fadeRenderer.sortingLayerName = "Background";
            fadeRenderer.sortingOrder = 3;
        }

        /// <summary>A vertical energy conduit whose pulses run upward, with a faint cyan light.</summary>
        public static void Conduit(Transform parent, float x, float bottomY, float height)
        {
            var frames = EchoArtFactory.LoadSprites($"{Pixel}/Core/Conduit_{{0:00}}.png", 4);
            var go = new GameObject("Conduit");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, bottomY + height * 0.5f, 0f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(frames[0].bounds.size.x, height);
            renderer.sortingLayerName = "Background";
            renderer.sortingOrder = 2;
            SetFrames(go.AddComponent<SpriteFlipbook>(), frames, 8f);
            AddPointLight(go, EnergyLight, 0.35f, 2.2f);
        }

        /// <summary>
        /// The Guardian's reactor set into the core's wall, calm (cyan) or overloaded (danger red),
        /// with its halo and light.
        /// </summary>
        public static GameObject Reactor(Transform parent, Vector2 center, bool overload)
        {
            var go = new GameObject(overload ? "Reactor_Overload" : "Reactor");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = EchoArtFactory.LoadSprite($"{Pixel}/Core/{(overload ? "Reactor_Overload" : "Reactor_Calm")}.png");
            renderer.sortingLayerName = "Midground";

            Color glow = overload ? DangerLight : EnergyLight;
            var halo = new GameObject("Halo");
            halo.transform.SetParent(go.transform, false);
            halo.transform.localScale = Vector3.one * 5f;
            var haloRenderer = halo.AddComponent<SpriteRenderer>();
            haloRenderer.sprite = EchoArtFactory.LoadSprite($"{Particles}/light_01.png");
            haloRenderer.sharedMaterial = EchoArtFactory.SoftGlowMaterial;
            haloRenderer.color = new Color(glow.r, glow.g, glow.b, 0.35f);
            haloRenderer.sortingLayerName = "Midground";
            haloRenderer.sortingOrder = -1;

            AddPointLight(go, glow, 1.1f, 6f);
            go.AddComponent<LightFlicker>();
            return go;
        }

        // --- props and hazards ------------------------------------------------------------------

        /// <summary>A lab prop standing on the floor (bottom-centre pivot).</summary>
        public static GameObject Prop(Transform parent, string spritePath, float x, float floorY, int order = 0)
        {
            var sprite = EchoArtFactory.LoadSprite(spritePath);
            return PlaceBottom(parent, sprite.name, sprite, new Vector2(x, floorY), "Midground", order);
        }

        public static string LabProp(string name) => $"{Lab}/{name}.png";
        public static string WarpedProp(string name) => $"{Props}/{name}.png";

        /// <summary>A ceiling light fixture: soft glow plus a 2D light, some of them faulty.</summary>
        public static void CeilingLamp(Transform parent, float x, float y, bool faulty) =>
            CeilingLamp(parent, x, y, faulty, LampLight);

        public static void CeilingLamp(Transform parent, float x, float y, bool faulty, Color color)
        {
            var go = new GameObject("CeilingLamp");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            AddPointLight(go, color, 0.55f, 4f);
            var flicker = go.AddComponent<LightFlicker>();
            if (faulty)
            {
                var settings = new SerializedObject(flicker);
                settings.FindProperty("faultInterval").floatValue = 3.5f;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>Warning stripes on both lips of a pit and a warm glow rising from below.</summary>
        public static void Pit(Transform parent, float xMin, float xMax, float floorY, float bottomY)
        {
            var root = new GameObject("PitWarning").transform;
            root.SetParent(parent, false);

            var stripe = EchoArtFactory.LoadSprite($"{Pixel}/Hazard/HazardStripe.png");
            float stripeHeight = stripe.bounds.size.y;
            foreach (float edge in new[] { xMin, xMax })
            {
                float side = edge == xMin ? -1f : 1f;
                var go = new GameObject("Stripes");
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(edge + side * 0.75f, floorY - 0.125f - stripeHeight * 0.5f, 0f);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = stripe;
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.size = new Vector2(1.5f, stripeHeight);
                renderer.sortingLayerName = "Level";
                renderer.sortingOrder = 10;
            }

            var glowSprite = EchoArtFactory.LoadSprite($"{Pixel}/Hazard/PitGlow.png");
            var glow = new GameObject("PitGlow");
            glow.transform.SetParent(root, false);
            float glowHeight = glowSprite.bounds.size.y;
            glow.transform.position = new Vector3((xMin + xMax) * 0.5f, bottomY + glowHeight * 0.5f, 0f);
            var glowRenderer = glow.AddComponent<SpriteRenderer>();
            glowRenderer.sprite = glowSprite;
            glowRenderer.drawMode = SpriteDrawMode.Tiled;
            glowRenderer.size = new Vector2(xMax - xMin, glowHeight);
            glowRenderer.sortingLayerName = "Hazards";

            var light = new GameObject("DangerLight");
            light.transform.SetParent(root, false);
            light.transform.position = new Vector3((xMin + xMax) * 0.5f, bottomY + 0.5f, 0f);
            AddPointLight(light, DangerLight, 0.9f, Mathf.Max(3f, (xMax - xMin) * 0.75f));
            light.AddComponent<LightFlicker>();
        }

        // --- interactables ----------------------------------------------------------------------

        /// <summary>Animated energy cell with a cyan halo and light, bobbing above the pickup's trigger.</summary>
        public static void DressEnergyCell(GameObject pickup)
        {
            var visual = new GameObject("Visual");
            visual.transform.SetParent(pickup.transform, false);
            visual.AddComponent<Bob>();

            var halo = new GameObject("Halo");
            halo.transform.SetParent(visual.transform, false);
            halo.transform.localScale = Vector3.one * 1.6f;
            var haloRenderer = halo.AddComponent<SpriteRenderer>();
            haloRenderer.sprite = EchoArtFactory.LoadSprite($"{Particles}/light_01.png");
            haloRenderer.sharedMaterial = EchoArtFactory.SoftGlowMaterial;
            haloRenderer.color = new Color(0.21f, 0.89f, 1f, 0.45f);
            haloRenderer.sortingLayerName = "Interactables";
            haloRenderer.sortingOrder = -1;

            var cell = new GameObject("Cell");
            cell.transform.SetParent(visual.transform, false);
            var cellRenderer = cell.AddComponent<SpriteRenderer>();
            var frames = EchoArtFactory.LoadSprites($"{Pixel}/Energy/EnergyCell_{{0:00}}.png", 4);
            cellRenderer.sprite = frames[0];
            cellRenderer.sortingLayerName = "Interactables";
            SetFrames(cell.AddComponent<SpriteFlipbook>(), frames, 8f);

            AddPointLight(visual, EnergyLight, 0.6f, 1.8f);
        }

        /// <summary>
        /// Energy barrier art: a scrolling field between two emitters. Returns the field, which
        /// the barrier hides when broken (the emitters stay, dead).
        /// </summary>
        public static GameObject DressBarrier(GameObject barrier, float height, bool isExit)
        {
            string fieldName = isExit ? "ExitField" : "BarrierField";
            var frames = EchoArtFactory.LoadSprites($"{Pixel}/Energy/{fieldName}_{{0:00}}.png", 4);

            var field = new GameObject("Field");
            field.transform.SetParent(barrier.transform, false);
            var renderer = field.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(frames[0].bounds.size.x, height);
            renderer.sortingLayerName = "Interactables";
            SetFrames(field.AddComponent<SpriteFlipbook>(), frames, 10f);
            AddPointLight(field, isExit ? ExitLight : EnergyLight, 0.8f, 2.5f);

            var emitter = EchoArtFactory.LoadSprite($"{Pixel}/Energy/BarrierEmitter.png");
            float emitterHalf = emitter.bounds.size.y * 0.5f;
            foreach (float y in new[] { height * 0.5f - emitterHalf, -height * 0.5f + emitterHalf })
            {
                var cap = new GameObject("Emitter");
                cap.transform.SetParent(barrier.transform, false);
                cap.transform.localPosition = new Vector3(0f, y, 0f);
                cap.transform.localScale = new Vector3(1f, y > 0f ? 1f : -1f, 1f);
                var capRenderer = cap.AddComponent<SpriteRenderer>();
                capRenderer.sprite = emitter;
                capRenderer.sortingLayerName = "Interactables";
                capRenderer.sortingOrder = 1;
            }
            return field;
        }

        /// <summary>
        /// The hero as scenery (title screen): her idle loop and personal light, no gameplay parts.
        /// </summary>
        public static GameObject Hero(Transform parent, Vector2 feet, bool faceLeft)
        {
            var frames = EchoArtFactory.EnsurePlayerVisuals().idle.frames;
            var go = new GameObject("Hero");
            go.transform.SetParent(parent, false);
            go.transform.position = feet;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.flipX = faceLeft;
            renderer.sortingLayerName = "Player";
            var flipbook = go.AddComponent<SpriteFlipbook>();
            SetFrames(flipbook, frames, EchoArtFactory.EnsurePlayerVisuals().idle.framesPerSecond);
            var settings = new SerializedObject(flipbook);
            settings.FindProperty("randomStart").boolValue = false;
            settings.ApplyModifiedPropertiesWithoutUndo();

            var glow = new GameObject("Glow");
            glow.transform.SetParent(go.transform, false);
            glow.transform.localPosition = new Vector3(0f, 1f, 0f);
            AddPointLight(glow, new Color(0.75f, 0.92f, 1f), 0.4f, 2.6f);
            return go;
        }

        // --- atmosphere -------------------------------------------------------------------------

        /// <summary>Slow dust motes drifting through the camera's view (follows the camera).</summary>
        public static void DustMotes(Transform cameraTransform)
        {
            var motes = new EchoFx("DustMotes", cameraTransform, EchoArtFactory.DustMaterial, "Foreground")
                .Looping(5f, 60)
                .Life(6f, 9f)
                .Size(0.03125f, 0.0625f)
                .Colors(new Color(0.55f, 0.8f, 0.85f, 0.35f), new Color(0.75f, 0.95f, 1f, 0.2f))
                .Box(new Vector2(24f, 13f))
                .Velocity(new Vector2(-0.12f, -0.05f), new Vector2(0.12f, 0.12f))
                .FadeInOut();
            motes.Transform.localPosition = new Vector3(0f, 0f, 10f);
        }

        public static Light2D CreateGlobalLight(float intensity, Color color)
        {
            var go = new GameObject("Global Light 2D");
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = intensity;
            light.color = color;
            ApplyToAllSortingLayers(light);
            return light;
        }

        public static Light2D AddPointLight(GameObject target, Color color, float intensity, float radius)
        {
            var light = target.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.pointLightOuterRadius = radius;
            light.pointLightInnerRadius = 0f;
            light.falloffIntensity = 0.7f;
            ApplyToAllSortingLayers(light);
            return light;
        }

        /// <summary>Light2D only fills its sorting layers in Awake, which edit-time AddComponent may skip.</summary>
        public static void ApplyToAllSortingLayers(Light2D light)
        {
            var layers = SortingLayer.layers;
            var serialized = new SerializedObject(light);
            var targets = serialized.FindProperty("m_ApplyToSortingLayers");
            targets.arraySize = layers.Length;
            for (int i = 0; i < layers.Length; i++)
                targets.GetArrayElementAtIndex(i).intValue = layers[i].id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // --- helpers ----------------------------------------------------------------------------

        static GameObject PlaceBottom(Transform parent, string name, Sprite sprite, Vector2 bottomCenter, string layer, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            // Lab art pivots are bottom-centre (EchoImportRules); others are centred.
            float pivotOffset = sprite.pivot.y / sprite.pixelsPerUnit;
            go.transform.position = new Vector3(bottomCenter.x, bottomCenter.y + pivotOffset, 0f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = layer;
            renderer.sortingOrder = order;
            return go;
        }

        static void SetFrames(SpriteFlipbook flipbook, Sprite[] frames, float fps)
        {
            var settings = new SerializedObject(flipbook);
            var array = settings.FindProperty("frames");
            array.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            settings.FindProperty("framesPerSecond").floatValue = fps;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetParallax(ParallaxLayer layer, Vector2 factor)
        {
            var settings = new SerializedObject(layer);
            settings.FindProperty("factor").vector2Value = factor;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
