using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Object = UnityEngine.Object;

namespace SecretOfHegra.EditorTools
{
    /// <summary>
    /// Tools ▸ Secret of Hegra ▸ Build or Repair Game. Idempotent: regenerates the whole scene from code
    /// (backing up any existing copy first), refreshes shared assets in place, and configures the project.
    /// </summary>
    public static partial class HegraBuilder
    {
        public const string ScenePath = HegraKit.Root + "/Scenes/SecretOfHegra.unity";
        const string SamplesRoot = "Assets/Samples/XR Interaction Toolkit/3.6.1";
        const string RigPrefabPath = SamplesRoot + "/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        const string VignettePrefabPath = SamplesRoot + "/Starter Assets/TunnelingVignette/TunnelingVignette.prefab";
        const string SimulatorPrefabPath = SamplesRoot + "/XR Interaction Simulator/XR Interaction Simulator.prefab";

        // Stop centers (world meters). The route winds north then east across a compact valley.
        public static readonly Vector3 CampPos = new Vector3(0, 0, 0);
        public static readonly Vector3 MarketPos = new Vector3(0, 0, 26);
        public static readonly Vector3 TheaterPos = new Vector3(22, 0, 46);
        public static readonly Vector3 IthlibPos = new Vector3(50, 0, 58);
        public static readonly Vector3 BintPos = new Vector3(78, 0, 44);
        public static readonly Vector3 FaridPos = new Vector3(100, 0, 18);

        static HegraMats M;
        static TMP_FontAsset s_Font, s_NabFont;

        [MenuItem("Tools/Secret of Hegra/Build or Repair Game", priority = 0)]
        public static void BuildOrRepairMenu()
        {
            var ok = BuildOrRepair(out var report);
            EditorUtility.DisplayDialog("Secret of Hegra", report, "OK");
            if (ok) Debug.Log("[Hegra] " + report);
        }

        /// <summary>Batch-mode entry: -executeMethod SecretOfHegra.EditorTools.HegraBuilder.BuildOrRepairBatch</summary>
        public static void BuildOrRepairBatch()
        {
            var ok = BuildOrRepair(out var report);
            Debug.Log("[Hegra] " + report);
            if (!ok) EditorApplication.Exit(1);
        }

        /// <summary>Entry point also usable from automation (returns false if a prerequisite import is pending).</summary>
        public static bool BuildOrRepair(out string report)
        {
            if (EditorApplication.isPlaying) { report = "Exit Play Mode first."; return false; }
            HegraKit.ResetCache();
            foreach (var f in new[] { "Scenes", "Materials", "Prefabs", "Textures", "Generated/Meshes", "Fonts", "Audio", "Data", "Docs" })
                HegraKit.EnsureFolder(HegraKit.Root + "/" + f);

            if (!EnsurePrerequisites(out report)) return false;

            M = HegraMats.Create();
            CreateFonts();
            AssetDatabase.SaveAssets();

            var audio = HegraAudioAssets.EnsureAll();
            ConfigureSimulatorSettings();
            HegraProjectSetup.ConfigureAll(false);

            BackupScene();
            BuildScene(audio);
            AddSceneToBuild();
            AssetDatabase.SaveAssets();

            var issues = HegraValidator.Validate(false);
            report = issues.Count == 0
                ? "Scene built and validated:\n" + ScenePath
                : $"Scene built: {ScenePath}\nValidation found {issues.Count} issue(s) — see Console.";
            return true;
        }

        // ───────────── prerequisites ─────────────

        static bool EnsurePrerequisites(out string report)
        {
            report = "";
            bool samples = File.Exists(RigPrefabPath) && File.Exists(SimulatorPrefabPath);
            if (!samples)
            {
                foreach (var s in UnityEditor.PackageManager.UI.Sample.FindByPackage("com.unity.xr.interaction.toolkit", "3.6.1"))
                    if (s.displayName == "Starter Assets" || s.displayName == "XR Interaction Simulator")
                        s.Import(UnityEditor.PackageManager.UI.Sample.ImportOptions.HideImportWindow | UnityEditor.PackageManager.UI.Sample.ImportOptions.OverridePreviousImports);
                report = "Imported XR Interaction Toolkit samples. Wait for compilation, then run Build or Repair again.";
                return false;
            }
            if (!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
            {
                TMP_PackageResourceImporter.ImportResources(true, false, false);
                report = "Imported TextMesh Pro essentials. Run Build or Repair again.";
                return false;
            }
            return true;
        }

        static void ConfigureSimulatorSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SimulatorPrefabPath);
            if (!settings || !prefab) { Debug.LogWarning("[Hegra] XR simulator settings not found; desktop testing needs the XR Interaction Simulator prefab in the scene."); return; }
            var so = new SerializedObject(settings);
            so.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab").boolValue = true;
            so.FindProperty("m_AutomaticallyInstantiateInEditorOnly").boolValue = true;
            so.FindProperty("m_UseClassic").boolValue = false;
            so.FindProperty("m_SimulatorPrefab").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ───────────── fonts ─────────────

        static void CreateFonts()
        {
            s_NabFont = FontAsset("HegraNabataeanPUA-Regular", "TMP_HegraNabataean", null);
            s_Font = FontAsset("Amiri-Regular", "TMP_Amiri", s_NabFont);
        }

        static TMP_FontAsset FontAsset(string ttf, string assetName, TMP_FontAsset fallback)
        {
            var path = $"{HegraKit.Root}/Fonts/{assetName}.asset";
            var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (!fa)
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>($"{HegraKit.Root}/Fonts/{ttf}.ttf");
                if (!font) { Debug.LogError($"[Hegra] Missing font file {ttf}.ttf"); return null; }
                fa = TMP_FontAsset.CreateFontAsset(font, 72, 8, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                fa.name = assetName;
                AssetDatabase.CreateAsset(fa, path);
                fa.atlasTextures[0].name = assetName + " Atlas";
                AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
                fa.material.name = assetName + " Material";
                AssetDatabase.AddObjectToAsset(fa.material, fa);
            }
            if (fallback)
            {
                fa.fallbackFontAssetTable ??= new List<TMP_FontAsset>();
                fa.fallbackFontAssetTable.RemoveAll(x => x == null);
                if (!fa.fallbackFontAssetTable.Contains(fallback)) fa.fallbackFontAssetTable.Add(fallback);
            }
            EditorUtility.SetDirty(fa);
            return fa;
        }

        // ───────────── scene ─────────────

        static void BackupScene()
        {
            if (!File.Exists(ScenePath)) return;
            var dir = "Backups_SecretOfHegra/Scenes";
            Directory.CreateDirectory(dir);
            var dest = Path.Combine(dir, $"SecretOfHegra_{DateTime.Now:yyyyMMdd_HHmmss}.unity");
            File.Copy(ScenePath, dest, true);
            Debug.Log($"[Hegra] Previous scene backed up to {dest}");
        }

        static void AddSceneToBuild()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            // keep the template sample scene in the list but disabled so it is not built for Quest
            for (int i = 1; i < scenes.Count; i++) scenes[i].enabled = false;
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void BuildScene(HegraAudioAssets.Set audio)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var world = new GameObject("World").transform;
            var systems = new GameObject("Systems").transform;

            SetupLighting(world);
            var moon = BuildSky(world);
            BuildGround(world);

            // Systems
            var im = new GameObject("XR Interaction Manager", typeof(XRInteractionManager));
            im.transform.SetParent(systems, false);
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(XRUIInputModule));
            es.transform.SetParent(systems, false);

            var rig = BuildRig(out var body, out var belt, out var satchel);

            var stops = new List<StopBase>();
            var camp = BuildCamp(world, audio);
            stops.Add(camp);
            stops.Add(BuildMarket(world, audio, satchel));
            stops.Add(BuildTheater(world, audio));
            stops.Add(BuildIthlib(world, audio));
            stops.Add(BuildBint(world, audio));
            stops.Add(BuildFarid(world, audio));
            BuildStopPlaceholders(world, audio, stops);
            BuildDressing(world, audio);

            var lamp = BuildLamp(camp.transform.Find("LampSpot"), belt);

            // the lamp starts every journey in the player's dominant hand and is hung on the belt in the lesson
            var carry = rig.gameObject.AddComponent<LampHandCarry>();
            carry.lamp = lamp;
            carry.belt = belt;
            carry.leftHand = rig.LeftController;
            carry.rightHand = rig.RightController;
            rig.gameObject.AddComponent<InteractionHaptics>();
            AirMotes(rig.transform);

            var gmGo = new GameObject("GameManager");
            gmGo.transform.SetParent(systems, false);
            var gm = gmGo.AddComponent<GameManager>();
            var dialogue = gmGo.AddComponent<DialogueRunner>();
            var voice = gmGo.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0f;
            var voBank = HegraAudioAssets.EnsureVoiceoverBank();
            dialogue.VoiceSource = voice;
            dialogue.Voiceover = voBank;
            gmGo.AddComponent<KohlEffect>();

            var audioGo = new GameObject("AudioDirector");
            audioGo.transform.SetParent(systems, false);
            var director = audioGo.AddComponent<AudioDirector>();
            HegraAudioAssets.Configure(director, audio);
            voice.outputAudioMixerGroup = audio.dialogueGroup;

            var menusGo = new GameObject("Menus");
            menusGo.transform.SetParent(systems, false);
            var menus = menusGo.AddComponent<MenuSystem>();

            moon.skyMaterial = M.sky;

            var kamkam = BuildKamkam(world);

            gm.ConfigureData(
                AssetDatabase.LoadAssetAtPath<TextAsset>(HegraKit.Root + "/Data/ui_strings.json"),
                AssetDatabase.LoadAssetAtPath<TextAsset>(HegraKit.Root + "/Data/dialogue.json"),
                AssetDatabase.LoadAssetAtPath<TextAsset>(HegraKit.Root + "/Data/nabataean_map.json"),
                s_Font, s_NabFont);
            gm.ConfigureSystems(rig, dialogue, director, menus, moon, body, belt, satchel, lamp, stops);
            EditorUtility.SetDirty(gm);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        static void SetupLighting(Transform world)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.16f, 0.19f, 0.32f);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.07f, 0.08f, 0.15f);
            // light haze: near rocks stay crisp, the horizon buttes (~280-330 m) read as soft silhouettes
            RenderSettings.fogStartDistance = 35f;
            RenderSettings.fogEndDistance = 430f;
            RenderSettings.skybox = M.sky;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.reflectionIntensity = 0.3f;

            var settingsPath = HegraKit.Root + "/Generated/HegraLighting.lighting";
            var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(settingsPath);
            if (!ls)
            {
                ls = new LightingSettings { name = "HegraLighting" };
                AssetDatabase.CreateAsset(ls, settingsPath);
            }
            ls.bakedGI = false;
            ls.realtimeGI = false;
            Lightmapping.lightingSettings = ls;
        }

        static MoonController BuildSky(Transform world)
        {
            var sky = HegraKit.Group("Sky", world, Vector3.zero).transform;
            var lightGo = new GameObject("MoonLight");
            lightGo.transform.SetParent(sky, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = HegraPalette.Moonlight;
            light.intensity = 0.55f;
            light.shadows = LightShadows.None;

            var disc = HegraKit.Prim(PrimitiveType.Sphere, sky, "MoonDisc", new Vector3(0, 200, 300), Vector3.one * 16f, M.moon);
            HegraKit.Prim(PrimitiveType.Quad, disc.transform, "Halo", Vector3.zero, Vector3.one * 4f, M.glowMoon);
            var stars = HegraKit.MeshObject("Stars", sky, HegraKit.StarField("StarField", 420, 330f, 7), M.glowStar, Vector3.zero);
            stars.isStatic = true;
            var mountains = HegraKit.MeshObject("Mountains", sky, HegraKit.MountainRing("MountainRing", 280f, 38f, 96, 5), M.mountainSandstone, new Vector3(50, 0, 30));
            HegraKit.MarkStatic(mountains);

            var moon = sky.gameObject.AddComponent<MoonController>();
            moon.moonLight = light;
            moon.moonDisc = disc.transform;
            return moon;
        }

        /// <summary>High + low detail meshes of one sandstone rock shape (same seed, same silhouette).</summary>
        static (Mesh high, Mesh low) RockMeshes(string name, int seed, int lon = 18, int lat = 12, float roughness = 0.22f, float flattenBottom = 0.25f)
            => (HegraKit.Rock(name, seed, lon, lat, roughness, flattenBottom), HegraKit.RockLow(name, seed, roughness, flattenBottom));

        /// <summary>
        /// A sandstone rock with an LODGroup (detailed up close, light far away) using the procedural sandstone
        /// material; the optional collider uses the low mesh, which is far cheaper for physics on Quest.
        /// </summary>
        static GameObject RockObject(string name, Transform parent, (Mesh high, Mesh low) meshes, Vector3 localPos, Vector3? euler, Vector3 scale, bool collider)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            root.transform.localEulerAngles = euler ?? Vector3.zero;
            root.transform.localScale = scale;
            var l0 = HegraKit.MeshObject(name + "_LOD0", root.transform, meshes.high, M.rockSandstone, Vector3.zero).GetComponent<Renderer>();
            var l1 = HegraKit.MeshObject(name + "_LOD1", root.transform, meshes.low, M.rockSandstone, Vector3.zero).GetComponent<Renderer>();
            var lods = root.AddComponent<LODGroup>();
            lods.SetLODs(new[] { new LOD(0.18f, new[] { l0 }), new LOD(0.0005f, new[] { l1 }) });
            lods.RecalculateBounds();
            if (collider) root.AddComponent<MeshCollider>().sharedMesh = meshes.low;
            return root;
        }

        static void BuildGround(Transform world)
        {
            var ground = HegraKit.Prim(PrimitiveType.Plane, world, "Ground", new Vector3(50, 0, 30), new Vector3(30, 1, 22), M.sand, null, true);
            M.sand.SetTextureScale("_BaseMap", new Vector2(40, 30));
            var area = ground.AddComponent<TeleportationArea>();
            area.interactionLayers = InteractionLayerMask.GetMask("Teleport");
            HegraKit.MarkStatic(ground);

            // scattered sandstone outcrops that frame the valley (static, no colliders except the larger ones)
            var rocks = HegraKit.Group("Outcrops", world, Vector3.zero).transform;
            var rnd = new System.Random(3);
            var rockMesh = RockMeshes("Outcrop", 41);
            var rockMesh2 = RockMeshes("Outcrop2", 77, 16, 10, 0.3f);
            Vector3[] spots =
            {
                new Vector3(-14, 0, 10), new Vector3(-12, 0, 38), new Vector3(8, 0, 62), new Vector3(36, 0, 78), new Vector3(66, 0, 74),
                new Vector3(96, 0, 60), new Vector3(118, 0, 36), new Vector3(122, 0, 0), new Vector3(80, 0, -6), new Vector3(46, 0, 10),
                new Vector3(20, 0, 14), new Vector3(-10, 0, -14), new Vector3(30, 0, 30), new Vector3(64, 0, 30)
            };
            foreach (var p in spots)
            {
                float s = 3f + (float)rnd.NextDouble() * 6f;
                RockObject("Rock", rocks, rnd.Next(2) == 0 ? rockMesh : rockMesh2, p,
                    new Vector3(0, (float)rnd.NextDouble() * 360f, 0), new Vector3(s * 1.3f, s, s), true);
            }
            HegraKit.MarkStatic(rocks.gameObject);
        }

        // ───────────── player rig ─────────────

        static PlayerRig BuildRig(out BodyAnchor body, out HegraSocket belt, out HegraSocket satchel)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefabPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = "XR Origin (Traveler)";
            var origin = go.GetComponent<XROrigin>();
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            var cam = origin.Camera;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 600f;
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = HegraPalette.Night;

            // comfort: no jumping, climbing or grab-move in this experience
            foreach (var n in new[] { "Locomotion/Jump", "Locomotion/Climb", "Locomotion/Grab Move" })
            {
                var t = go.transform.Find(n);
                if (t) t.gameObject.SetActive(false);
            }

            // comfort fade quad
            var fadeQuad = HegraKit.Prim(PrimitiveType.Quad, cam.transform, "FadeQuad", new Vector3(0, 0, 0.12f), Vector3.one * 1.2f, M.fade);
            var fader = go.AddComponent<ScreenFader>();
            fader.Quad = fadeQuad.GetComponent<Renderer>();

            // tunneling vignette (used only with smooth movement)
            TunnelingVignetteController vignette = null;
            var vigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VignettePrefabPath);
            if (vigPrefab)
            {
                var vig = (GameObject)PrefabUtility.InstantiatePrefab(vigPrefab, cam.transform);
                vignette = vig.GetComponent<TunnelingVignetteController>();
            }

            var snap = go.GetComponentInChildren<SnapTurnProvider>(true);
            var cont = go.GetComponentInChildren<ContinuousTurnProvider>(true);
            var move = go.GetComponentInChildren<ContinuousMoveProvider>(true);
            var tp = go.GetComponentInChildren<TeleportationProvider>(true);
            if (vignette)
            {
                vignette.locomotionVignetteProviders = new List<LocomotionVignetteProvider>
                {
                    new LocomotionVignetteProvider { locomotionProvider = move, enabled = true },
                    new LocomotionVignetteProvider { locomotionProvider = cont, enabled = true },
                };
            }

            var rig = go.AddComponent<PlayerRig>();
            rig.Configure(origin, cam, go.transform.Find("Camera Offset/Left Controller"), go.transform.Find("Camera Offset/Right Controller"),
                snap, cont, move, tp, vignette, fader);

            // waist anchor with the lamp belt hook and the satchel
            var anchorGo = new GameObject("BodyAnchor");
            anchorGo.transform.SetParent(go.transform, false);
            body = anchorGo.AddComponent<BodyAnchor>();
            body.head = cam.transform;
            body.floorReference = go.transform;

            belt = MakeSocket(anchorGo.transform, "BeltHook", new[] { ItemKind.Lamp }, 0.13f);
            HegraKit.Prim(PrimitiveType.Cylinder, belt.transform, "Hook", new Vector3(0, -0.03f, 0), new Vector3(0.05f, 0.01f, 0.05f), M.bronze);
            satchel = MakeSocket(anchorGo.transform, "Satchel", new[] { ItemKind.NameShard }, 0.12f);
            HegraKit.Prim(PrimitiveType.Cube, satchel.transform, "Bag", new Vector3(0, -0.08f, -0.02f), new Vector3(0.16f, 0.14f, 0.06f), M.leather);
            HegraKit.Prim(PrimitiveType.Cube, anchorGo.transform, "Belt", new Vector3(0, 0, 0.02f), new Vector3(0.34f, 0.04f, 0.02f), M.leather);
            body.beltHook = belt.transform;
            body.satchel = satchel.transform;

            HegraHands.Build(go.transform.Find("Camera Offset/Left Controller"), go.transform.Find("Camera Offset/Right Controller"), M);
            return rig;
        }

        static HegraSocket MakeSocket(Transform parent, string name, ItemKind[] accepts, float radius)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = radius;
            var socket = go.AddComponent<HegraSocket>();
            socket.Accepts = accepts;
            var attach = new GameObject("Attach").transform;
            attach.SetParent(go.transform, false);
            socket.attachTransform = attach;
            socket.showInteractableHoverMeshes = true;
            return socket;
        }

        // ───────────── clay lamp prefab ─────────────

        static HegraItem BuildLamp(Transform spot, HegraSocket belt)
        {
            var path = HegraKit.PrefabDir + "/ClayLamp.prefab";
            var root = new GameObject("ClayLamp");
            HegraKit.Prim(PrimitiveType.Sphere, root.transform, "Body", Vector3.zero, new Vector3(0.11f, 0.05f, 0.08f), M.clay);
            HegraKit.Prim(PrimitiveType.Cylinder, root.transform, "Nozzle", new Vector3(0, 0.005f, 0.055f), new Vector3(0.03f, 0.02f, 0.03f), M.clay, new Vector3(80, 0, 0));
            HegraKit.Prim(PrimitiveType.Capsule, root.transform, "Handle", new Vector3(0, 0.02f, -0.06f), new Vector3(0.018f, 0.03f, 0.018f), M.clay, new Vector3(-40, 0, 0));
            HegraKit.Prim(PrimitiveType.Cylinder, root.transform, "Oil", new Vector3(0, 0.024f, -0.005f), new Vector3(0.035f, 0.002f, 0.035f), M.kohlBlack);
            var flame = new GameObject("Flame").transform;
            flame.SetParent(root.transform, false);
            flame.localPosition = new Vector3(0, 0.035f, 0.07f);
            HegraKit.Prim(PrimitiveType.Sphere, flame, "Core", new Vector3(0, 0.018f, 0), new Vector3(0.016f, 0.04f, 0.016f), M.flame);
            HegraKit.Prim(PrimitiveType.Quad, flame, "Glow", new Vector3(0, 0.02f, 0), new Vector3(0.12f, 0.16f, 0.12f), M.glowFire).AddComponent<FlameFlicker>().billboardYaw = true;
            var lightGo = new GameObject("LampLight");
            lightGo.transform.SetParent(flame, false);
            lightGo.transform.localPosition = new Vector3(0, 0.06f, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.68f, 0.35f);
            light.range = 4f;
            light.intensity = 1.4f;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;

            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(0.12f, 0.07f, 0.17f);
            col.center = new Vector3(0, 0.005f, 0);
            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.4f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var grab = root.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grab.throwOnDetach = false;
            grab.useDynamicAttach = true;
            var item = root.AddComponent<HegraItem>();
            item.kind = ItemKind.Lamp;
            var lamp = root.AddComponent<ClayLamp>();
            lamp.Light = light;
            lamp.Flame = flame;
            lamp.RevealOrigin = flame;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.transform.SetPositionAndRotation(spot.position, spot.rotation);
            var instItem = inst.GetComponent<HegraItem>();
            instItem.homeSocket = belt;
            // never lost: straight back to the belt if it falls through the ground, or after 2 s lying loose
            instItem.killY = -0.5f;
            instItem.looseReturnSeconds = 2f;
            return instItem;
        }
    }
}
