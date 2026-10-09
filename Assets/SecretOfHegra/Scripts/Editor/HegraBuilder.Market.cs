using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SecretOfHegra.EditorTools
{
    public static partial class HegraBuilder
    {
        // ───────────── shared helpers for interactable props ─────────────

        /// <summary>Saves a built object as a prefab and returns a scene instance at the same pose.</summary>
        static GameObject SavePrefab(GameObject root, string name)
        {
            var path = $"{HegraKit.PrefabDir}/{name}.prefab";
            var pos = root.transform.position; var rot = root.transform.rotation; var parent = root.transform.parent;
            root.transform.SetParent(null, true);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.transform.SetPositionAndRotation(pos, rot);
            return inst;
        }

        static XRGrabInteractable MakeGrabbable(GameObject go, float mass, ItemKind kind, bool essential, Vector3 colliderSize, Vector3 colliderCenter = default)
        {
            var col = go.AddComponent<BoxCollider>();
            col.size = colliderSize;
            col.center = colliderCenter;
            var rb = Ensure<Rigidbody>(go);
            rb.mass = mass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var grab = Ensure<XRGrabInteractable>(go);
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grab.throwOnDetach = false;
            grab.useDynamicAttach = true;
            var item = Ensure<HegraItem>(go);
            item.kind = kind;
            item.essential = essential;
            return grab;
        }

        static T Ensure<T>(GameObject go) where T : Component => go.GetComponent<T>() ? go.GetComponent<T>() : go.AddComponent<T>();

        static Transform Stall(Transform parent, string name, Vector3 pos, float yaw, Material awning, float width = 2.2f)
        {
            var g = HegraKit.Group(name, parent, pos, yaw).transform;
            float w = width * 0.5f;
            foreach (var x in new[] { -w, w })
            {
                HegraKit.Prim(PrimitiveType.Cylinder, g, "PoleBack", new Vector3(x, 1.15f, -0.6f), new Vector3(0.07f, 1.15f, 0.07f), M.wood);
                HegraKit.Prim(PrimitiveType.Cylinder, g, "PoleFront", new Vector3(x, 1.05f, 0.95f), new Vector3(0.07f, 1.05f, 0.07f), M.wood);
            }
            HegraKit.Prim(PrimitiveType.Cube, g, "Awning", new Vector3(0, 2.2f, 0.18f), new Vector3(width + 0.35f, 0.04f, 1.9f), awning, new Vector3(-8f, 0, 0));
            HegraKit.Prim(PrimitiveType.Cube, g, "AwningStripe", new Vector3(0, 2.23f, 0.18f), new Vector3(0.3f, 0.045f, 1.92f), M.linen, new Vector3(-8f, 0, 0));
            HegraKit.Prim(PrimitiveType.Cube, g, "Counter", new Vector3(0, 0.475f, 0.6f), new Vector3(width, 0.95f, 0.6f), M.wood, null, true);
            HegraKit.Prim(PrimitiveType.Cube, g, "CounterCloth", new Vector3(0, 0.6f, 0.905f), new Vector3(width * 0.98f, 0.5f, 0.02f), awning);
            HegraKit.Prim(PrimitiveType.Cube, g, "Shelf", new Vector3(0, 1.3f, -0.55f), new Vector3(width, 0.05f, 0.35f), M.wood);
            HegraKit.Prim(PrimitiveType.Cube, g, "Back", new Vector3(0, 1.1f, -0.75f), new Vector3(width, 2.2f, 0.05f), M.tent);
            Lantern(g, new Vector3(w + 0.15f, 0, 1.05f), 2.0f);
            return g;
        }

        static void Pot(Transform parent, Vector3 local, float s, Material mat)
        {
            var g = HegraKit.Group("Pot", parent, local).transform;
            HegraKit.Prim(PrimitiveType.Sphere, g, "Belly", new Vector3(0, 0.1f, 0) * s, new Vector3(0.22f, 0.2f, 0.22f) * s, mat);
            HegraKit.Prim(PrimitiveType.Cylinder, g, "Rim", new Vector3(0, 0.2f, 0) * s, new Vector3(0.14f, 0.02f, 0.14f) * s, mat);
        }

        static HegraCharacters.Figure CrowdFigure(Transform parent, Vector3 pos, float yaw, Material robe, Material head, Material skin, float h)
        {
            var f = HegraCharacters.Build("Townsperson", parent, pos, yaw, robe, skin, h);
            HegraCharacters.Headcloth(f, head);
            f.npc.facePlayer = false;
            return f;
        }

        // ───────────── Stop 2: Market ─────────────

        static MarketStop BuildMarket(Transform world, HegraAudioAssets.Set audio, HegraSocket satchel)
        {
            var root = HegraKit.Group("Stop2_Market", world, MarketPos).transform;
            var stop = root.gameObject.AddComponent<MarketStop>();
            stop.stopId = StopId.Market;
            stop.spawnPoint = Point(root, "SpawnPoint", new Vector3(0, 0, -5f), Vector3.zero);

            var decor = HegraKit.Group("Decor", root, Vector3.zero).transform;
            HegraKit.Prim(PrimitiveType.Cube, decor, "Plaza", new Vector3(0, 0.004f, 2.5f), new Vector3(12f, 0.01f, 14f), M.sandstoneShadow);

            // Kamkam's kohl stall (east side, facing the path)
            var kStall = Stall(root, "KamkamStall", new Vector3(3.4f, 0, 1.5f), -90f, M.violet);
            stop.kamkamStall = kStall;
            stop.guidePoint = Point(kStall, "VendorSpot", new Vector3(0, 0, -0.2f), new Vector3(0, 0, 2f));
            for (int i = 0; i < 6; i++)
                HegraKit.Prim(PrimitiveType.Cylinder, kStall, "KohlDisplay", new Vector3(-0.8f + i * 0.32f, 1.38f, -0.55f), new Vector3(0.07f, 0.05f, 0.07f), i % 2 == 0 ? M.kohlBlack : M.bronze);
            var dish = HegraKit.Prim(PrimitiveType.Cylinder, kStall, "CoinDish", new Vector3(-0.6f, 0.96f, 0.78f), new Vector3(0.16f, 0.012f, 0.16f), M.clay);
            stop.coinDish = dish.transform;
            var tray = HegraKit.Prim(PrimitiveType.Cylinder, kStall, "BrassTray", new Vector3(0.1f, 0.96f, 0.72f), new Vector3(0.3f, 0.01f, 0.3f), M.bronze);
            var trayZone = new GameObject("PayZone");
            trayZone.transform.SetParent(kStall, false);
            trayZone.transform.localPosition = new Vector3(0.1f, 1.0f, 0.72f);
            var tz = trayZone.AddComponent<BoxCollider>();
            tz.isTrigger = true;
            tz.size = new Vector3(0.32f, 0.12f, 0.32f);
            stop.payTray = trayZone.AddComponent<PayTray>();

            // bronze mirror with a painted portrait (cheap "reflection")
            var mirrorRoot = HegraKit.Group("BronzeMirror", kStall, new Vector3(0.75f, 0.96f, 0.55f), 0f).transform;
            HegraKit.Prim(PrimitiveType.Cylinder, mirrorRoot, "Stand", new Vector3(0, 0.12f, 0), new Vector3(0.03f, 0.12f, 0.03f), M.wood);
            var disc = HegraKit.Prim(PrimitiveType.Cylinder, mirrorRoot, "Disc", new Vector3(0, 0.38f, 0), new Vector3(0.32f, 0.01f, 0.32f), M.bronze, new Vector3(-75f, 0, 0));
            var portrait = HegraKit.Prim(PrimitiveType.Quad, mirrorRoot, "Portrait", new Vector3(0, 0.38f, 0.013f), new Vector3(0.24f, 0.24f, 1f), HegraKit.Lit("M_MirrorPortrait", Color.white, null, 0.6f), new Vector3(15f, 180f, 0));
            var mirror = mirrorRoot.gameObject.AddComponent<BronzeMirror>();
            mirror.portrait = portrait.GetComponent<Renderer>();
            stop.mirror = mirror;

            // the coin the player pays with
            var coinGo = new GameObject("PaymentCoin");
            coinGo.transform.SetParent(kStall, false);
            coinGo.transform.localPosition = new Vector3(-0.6f, 0.99f, 0.78f);
            HegraKit.Prim(PrimitiveType.Cylinder, coinGo.transform, "Disc", Vector3.zero, new Vector3(0.04f, 0.003f, 0.04f), M.coinGold);
            MakeGrabbable(coinGo, 0.05f, ItemKind.Coin, false, new Vector3(0.045f, 0.012f, 0.045f));
            stop.coin = SavePrefab(coinGo, "Coin").GetComponent<HegraItem>();

            // kohl jar
            var jar = new GameObject("KohlJar");
            jar.transform.SetParent(kStall, false);
            jar.transform.localPosition = new Vector3(-0.2f, 1.02f, 0.75f);
            HegraKit.Prim(PrimitiveType.Sphere, jar.transform, "Pot", Vector3.zero, new Vector3(0.07f, 0.08f, 0.07f), M.kohlBlack);
            HegraKit.Prim(PrimitiveType.Cylinder, jar.transform, "Lid", new Vector3(0, 0.045f, 0), new Vector3(0.05f, 0.008f, 0.05f), M.bronze);
            HegraKit.Prim(PrimitiveType.Cylinder, jar.transform, "Stick", new Vector3(0, 0.08f, 0), new Vector3(0.006f, 0.04f, 0.006f), M.bronze);
            var bar = HegraKit.Prim(PrimitiveType.Cube, jar.transform, "ProgressBar", new Vector3(0, 0.14f, 0), new Vector3(0.12f, 0.012f, 0.004f), M.lanternBody);
            MakeGrabbable(jar, 0.15f, ItemKind.KohlJar, true, new Vector3(0.08f, 0.12f, 0.08f), new Vector3(0, 0.02f, 0));
            var app = jar.AddComponent<KohlApplicator>();
            app.progressBar = bar.transform;
            var jarInst = SavePrefab(jar, "KohlJar");
            stop.kohlJar = jarInst.GetComponent<HegraItem>();
            stop.kohl = jarInst.GetComponent<KohlApplicator>();

            // Wahballahi's workshop (west side)
            var wStall = Stall(root, "WahballahiWorkshop", new Vector3(-3.4f, 0, 4.5f), 90f, M.clothSaffron);
            for (int i = 0; i < 3; i++)
                HegraKit.Prim(PrimitiveType.Cube, wStall, "StoneBlock", new Vector3(-0.7f + i * 0.6f, 0.2f, -0.35f), new Vector3(0.45f, 0.4f, 0.35f), M.sandstoneLit, new Vector3(0, i * 17f, 0), true);
            HegraKit.Prim(PrimitiveType.Cube, wStall, "CarvedTablet", new Vector3(-0.6f, 1.6f, -0.7f), new Vector3(0.6f, 0.4f, 0.04f), M.sandstoneLit);
            var wPos = wStall.TransformPoint(new Vector3(0.4f, 0, -0.25f));
            var wf = HegraCharacters.Build("Wahballahi", root, root.InverseTransformPoint(wPos), 90f, M.linen, M.skinB, 1.74f);
            HegraCharacters.Apron(wf, M.leather);
            HegraCharacters.Headband(wf, M.clothRed);
            HegraCharacters.Belt(wf, M.leather);
            wf.npc.speakerId = "wahballahi";
            SpeakerExtras(wf.npc);
            wf.npc.idleGesture = NpcGesture.Carve;
            stop.wahballahi = wf.npc;

            stop.keyboardAnchor = Point(wStall, "KeyboardAnchor", new Vector3(0, 1.48f, 0.72f), new Vector3(0, 1.48f, -2f));
            stop.keyboardAnchor.localRotation = Quaternion.Euler(12f, 180f, 0f);
            stop.shardBenchPose = Point(wStall, "ShardBenchPose", new Vector3(0, 1.02f, 0.62f), Vector3.zero);
            stop.shardBenchPose.localRotation = Quaternion.Euler(35f, 180f, 0f);

            // name shard (key item for the finale)
            var shardGo = new GameObject("NameShard");
            shardGo.transform.SetParent(wStall, false);
            shardGo.transform.localPosition = new Vector3(0, 1.02f, 0.62f);
            shardGo.transform.localRotation = Quaternion.Euler(35f, 180f, 0f);
            HegraKit.Prim(PrimitiveType.Cube, shardGo.transform, "Clay", Vector3.zero, new Vector3(0.26f, 0.15f, 0.022f), M.clay);
            HegraKit.Prim(PrimitiveType.Cube, shardGo.transform, "ChipL", new Vector3(-0.12f, 0.06f, 0), new Vector3(0.05f, 0.05f, 0.021f), M.clay, new Vector3(0, 0, 30f));
            var surface = new GameObject("EngraveSurface").transform;
            surface.SetParent(shardGo.transform, false);
            var ns = shardGo.AddComponent<NameShard>();
            ns.nabataeanText = ShardText(shardGo.transform, "Nabataean", new Vector3(0, 0.022f, -0.0125f), 0.42f, new Color(0.25f, 0.12f, 0.08f));
            ns.modernText = ShardText(shardGo.transform, "Modern", new Vector3(0, -0.035f, -0.0125f), 0.22f, new Color(0.3f, 0.16f, 0.1f));
            ns.noteText = ShardText(shardGo.transform, "Note", new Vector3(0, -0.062f, -0.0125f), 0.075f, new Color(0.35f, 0.2f, 0.14f));
            ns.nabataeanText.font = s_NabFont;
            MakeGrabbable(shardGo, 0.3f, ItemKind.NameShard, true, new Vector3(0.26f, 0.15f, 0.03f));
            var shardInst = SavePrefab(shardGo, "NameShard");
            stop.shard = shardInst.GetComponent<NameShard>();
            stop.shard.GetComponent<HegraItem>().homeSocket = satchel;

            // chisel
            var chisel = new GameObject("Chisel");
            chisel.transform.SetParent(wStall, false);
            chisel.transform.localPosition = new Vector3(0.55f, 0.98f, 0.55f);
            chisel.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            HegraKit.Prim(PrimitiveType.Cylinder, chisel.transform, "Handle", new Vector3(0, -0.05f, 0), new Vector3(0.025f, 0.06f, 0.025f), M.wood);
            HegraKit.Prim(PrimitiveType.Cube, chisel.transform, "Blade", new Vector3(0, 0.05f, 0), new Vector3(0.012f, 0.08f, 0.006f), M.bronze);
            var tip = new GameObject("Tip").transform;
            tip.SetParent(chisel.transform, false);
            tip.localPosition = new Vector3(0, 0.09f, 0);
            MakeGrabbable(chisel, 0.2f, ItemKind.Chisel, false, new Vector3(0.03f, 0.22f, 0.03f));
            var chiselInst = SavePrefab(chisel, "Chisel");
            stop.chisel = chiselInst.GetComponent<HegraItem>();

            var engr = wStall.gameObject.AddComponent<ShardEngraver>();
            engr.shard = stop.shard;
            engr.surface = stop.shard.transform.Find("EngraveSurface");
            engr.chiselTip = chiselInst.transform.Find("Tip");
            engr.pointMaterial = M.glowMarker;
            engr.doneMaterial = M.ember;
            var lr = wStall.gameObject.AddComponent<LineRenderer>();
            lr.widthMultiplier = 0.004f;
            lr.sharedMaterial = M.lanternBody;
            lr.positionCount = 0;
            engr.guide = lr;
            stop.engraver = engr;

            // other stalls: pottery, cloth, incense
            var pottery = Stall(root, "PotteryStall", new Vector3(-3.6f, 0, -1.2f), 90f, M.clothRed);
            for (int i = 0; i < 5; i++) Pot(pottery, new Vector3(-0.8f + i * 0.4f, 0.95f, 0.6f), 0.9f + (i % 2) * 0.3f, M.pottery);
            Amphora(pottery, new Vector3(1.3f, 0, 1.2f)); Amphora(pottery, new Vector3(1.0f, 0, 1.5f), 0.8f);
            var cloth = Stall(root, "ClothStall", new Vector3(3.6f, 0, 5.8f), -90f, M.clothGreen);
            var cmats = new[] { M.clothRed, M.indigo, M.clothSaffron, M.violet, M.linen };
            for (int i = 0; i < 5; i++)
                HegraKit.Prim(PrimitiveType.Cylinder, cloth, "ClothRoll", new Vector3(-0.8f + i * 0.4f, 1.02f, 0.6f), new Vector3(0.12f, 0.25f, 0.12f), cmats[i], new Vector3(0, 0, 90f));
            var incense = Stall(root, "IncenseStall", new Vector3(0, 0, 10f), 180f, M.linen);
            for (int i = 0; i < 4; i++)
            {
                HegraKit.Prim(PrimitiveType.Cylinder, incense, "Basket", new Vector3(-0.75f + i * 0.5f, 1.02f, 0.6f), new Vector3(0.3f, 0.07f, 0.3f), M.rope);
                HegraKit.Prim(PrimitiveType.Sphere, incense, "Resin", new Vector3(-0.75f + i * 0.5f, 1.08f, 0.6f), new Vector3(0.22f, 0.06f, 0.22f), i % 2 == 0 ? M.resin : M.clothSaffron);
            }

            // musicians (frame drum + clapping) with a spatial percussion loop
            var m1 = HegraCharacters.Build("Drummer", root, new Vector3(-1.7f, 0, 7.6f), 160f, M.clothRed, M.skinA, 1.7f);
            HegraCharacters.Headcloth(m1, M.linen);
            HegraKit.Prim(PrimitiveType.Cylinder, m1.leftHand, "FrameDrum", new Vector3(0.6f, 1.2f, 1.6f), new Vector3(4.5f, 0.3f, 4.5f), M.leather, new Vector3(70, 0, 0));
            m1.npc.idleGesture = NpcGesture.Drum; m1.npc.facePlayer = false;
            var m2 = HegraCharacters.Build("Clapper", root, new Vector3(1.5f, 0, 7.8f), 200f, M.indigo, M.skinB, 1.62f);
            HegraCharacters.Veil(m2, M.clothSaffron);
            m2.npc.idleGesture = NpcGesture.Clap; m2.npc.facePlayer = false;
            Loop3D(root, "MarketPercussion", audio.Get("amb_market"), audio.ambienceGroup, 0.75f, 18f).transform.localPosition = new Vector3(0, 1.2f, 7.8f);

            // a small stylized crowd
            var robes = new[] { M.earth, M.clothGreen, M.indigo, M.clothRed, M.linen, M.tent, M.violet };
            var heads = new[] { M.linen, M.clothSaffron, M.earth, M.indigo };
            Vector3[] spots = { new Vector3(-1.2f, 0, 2.6f), new Vector3(1.6f, 0, 4.4f), new Vector3(-2.2f, 0, 6.2f), new Vector3(2.4f, 0, -1.4f), new Vector3(-0.6f, 0, 11.6f), new Vector3(1.2f, 0, 11.8f), new Vector3(-4.8f, 0, 2.2f) };
            float[] yaws = { 60f, -100f, 120f, -60f, 200f, 170f, 80f };
            var crowd = new List<HegraCharacters.Figure>();
            for (int i = 0; i < spots.Length; i++)
                crowd.Add(CrowdFigure(root, spots[i], yaws[i], robes[i % robes.Length], heads[i % heads.Length], i % 2 == 0 ? M.skinA : M.skinB, 1.55f + (i % 3) * 0.1f));
            BuildMarketChatter(root, crowd, audio);

            Lantern(root, new Vector3(-1.2f, 0, -3.5f));
            Lantern(root, new Vector3(1.2f, 0, -3.5f));
            Lantern(root, new Vector3(0f, 0, 6.2f), 2.4f);
            HegraKit.MarkStatic(decor.gameObject);

            var zone = root.gameObject.AddComponent<AmbienceZone>();
            zone.stop = StopId.Market;
            zone.radius = 13f;
            zone.music = null; // journey music plays throughout (AudioDirector)

            EduCardObject(root, new Vector3(-1.8f, 0, -3.2f), "edu.market");
            return stop;
        }

        static TextMeshPro ShardText(Transform parent, string name, Vector3 local, float size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            var t = go.AddComponent<TextMeshPro>();
            t.font = s_Font;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.rectTransform.sizeDelta = new Vector2(0.24f, 0.05f);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.enableAutoSizing = false;
            t.text = "";
            return t;
        }

        // ───────── townspeople who speak (silent text bubbles) ─────────

        static readonly string[][] s_ChatterLines =
        {
            new[] { "chatter.greet.1", "chatter.goods.1", "chatter.lore.1" },
            new[] { "chatter.greet.2", "chatter.goods.2", "chatter.lore.2" },
            new[] { "chatter.greet.3", "chatter.goods.3" },
            new[] { "chatter.greet.4", "chatter.goods.4", "chatter.lore.3" },
            new[] { "chatter.greet.1", "chatter.goods.5" },
            new[] { "chatter.greet.2", "chatter.goods.6", "chatter.lore.1" },
            new[] { "chatter.greet.3", "chatter.lore.2" },
        };
        static readonly float[] s_ChatterPitch = { 1.05f, 1.45f, 0.85f, 1.25f, 0.95f, 1.6f, 0.75f };

        static void BuildMarketChatter(Transform root, List<HegraCharacters.Figure> crowd, HegraAudioAssets.Set audio)
        {
            var chatter = root.gameObject.AddComponent<MarketChatter>();
            // silent chatter: speech bubbles and gestures only, no babble voices (the drums and music still play)
            chatter.syllables = new AudioClip[0];
            var back = HegraKit.Unlit("M_BubbleBack", new Color(0.07f, 0.08f, 0.15f));
            for (int i = 0; i < crowd.Count; i++)
            {
                var f = crowd[i];
                var headY = f.head ? f.head.position.y - f.root.transform.position.y : 1.6f;
                var bubbleRoot = HegraKit.Group("SpeechBubble", f.root.transform, new Vector3(0, headY + 0.42f, 0)).transform;
                var text = WorldText(bubbleRoot, "Text", Vector3.zero, 1.5f, null, HegraPalette.Linen, 1.3f);
                text.rectTransform.sizeDelta = new Vector2(1.3f, 0.5f);
                var quad = HegraKit.Prim(PrimitiveType.Quad, bubbleRoot, "Back", new Vector3(0, 0, 0.01f), new Vector3(1f, 0.3f, 1f), back);
                chatter.speakers.Add(new MarketChatter.Speaker
                {
                    npc = f.npc,
                    lineKeys = s_ChatterLines[i % s_ChatterLines.Length],
                    pitch = s_ChatterPitch[i % s_ChatterPitch.Length],
                    bubble = text,
                    bubbleRoot = bubbleRoot,
                    bubbleBack = quad.GetComponent<Renderer>(),
                    voice = null,
                });
                bubbleRoot.gameObject.SetActive(false);
            }

            // optional real recordings: Audio/Chatter/<line key>_ar.wav / _en.wav replace the babble
            var dir = HegraKit.Root + "/Audio/Chatter";
            if (AssetDatabase.IsValidFolder(dir))
                foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { dir }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var file = System.IO.Path.GetFileNameWithoutExtension(path);
                    int us = file.LastIndexOf('_');
                    if (us <= 0) continue;
                    var lang = file.Substring(us + 1).ToLowerInvariant();
                    if (lang != "ar" && lang != "en") continue;
                    chatter.recorded.Add(new MarketChatter.RecordedLine
                    {
                        key = file.Substring(0, us),
                        language = lang == "ar" ? Language.Arabic : Language.English,
                        clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path),
                    });
                }

            // no crowd murmur loop (no talking sounds in the market)
        }
    }
}
