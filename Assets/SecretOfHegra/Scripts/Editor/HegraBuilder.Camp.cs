using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SecretOfHegra.EditorTools
{
    public static partial class HegraBuilder
    {
        // ───────────── shared set pieces ─────────────

        /// <summary>Lantern on a pole: emissive body + billboard halo + light pool on the ground (no realtime light).</summary>
        static void Lantern(Transform parent, Vector3 localPos, float height = 2.1f, bool pole = true)
        {
            var g = HegraKit.Group("Lantern", parent, localPos).transform;
            if (pole) HegraKit.Prim(PrimitiveType.Cylinder, g, "Pole", new Vector3(0, height * 0.5f, 0), new Vector3(0.06f, height * 0.5f, 0.06f), M.wood);
            HegraKit.Prim(PrimitiveType.Cube, g, "Body", new Vector3(0, height + 0.12f, 0), new Vector3(0.16f, 0.22f, 0.16f), M.lanternBody);
            HegraKit.Prim(PrimitiveType.Cube, g, "Cap", new Vector3(0, height + 0.26f, 0), new Vector3(0.22f, 0.04f, 0.22f), M.bronze);
            var halo = HegraKit.Prim(PrimitiveType.Quad, g, "Halo", new Vector3(0, height + 0.12f, 0), new Vector3(0.9f, 0.9f, 0.9f), M.glowWarm);
            var fl = halo.AddComponent<FlameFlicker>();
            fl.billboardYaw = true;
            fl.amount = 0.06f;
            HegraKit.MeshObject("LightPool", g, HegraKit.GroundQuad(), M.glowPool, new Vector3(0, 0.02f, 0), null, new Vector3(5f, 1f, 5f));
        }

        static void Campfire(Transform parent, Vector3 localPos, HegraAudioAssets.Set audio)
        {
            var g = HegraKit.Group("Campfire", parent, localPos).transform;
            for (int i = 0; i < 5; i++)
                HegraKit.Prim(PrimitiveType.Cylinder, g, "Log" + i, new Vector3(0, 0.08f, 0), new Vector3(0.09f, 0.35f, 0.09f), M.wood, new Vector3(75, i * 72f, 0));
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                HegraKit.Prim(PrimitiveType.Sphere, g, "Stone" + i, new Vector3(Mathf.Cos(a) * 0.5f, 0.06f, Mathf.Sin(a) * 0.5f), new Vector3(0.22f, 0.14f, 0.2f), M.sandstoneShadow);
            }
            HegraKit.Prim(PrimitiveType.Sphere, g, "Embers", new Vector3(0, 0.08f, 0), new Vector3(0.4f, 0.1f, 0.4f), M.ember);
            var f1 = HegraKit.Prim(PrimitiveType.Quad, g, "Flame", new Vector3(0, 0.45f, 0), new Vector3(0.7f, 0.9f, 0.7f), M.glowFire);
            f1.AddComponent<FlameFlicker>().billboardYaw = true;
            var f2 = HegraKit.Prim(PrimitiveType.Quad, g, "FlameCore", new Vector3(0, 0.35f, 0), new Vector3(0.4f, 0.6f, 0.4f), M.glowFire);
            var fl2 = f2.AddComponent<FlameFlicker>(); fl2.billboardYaw = true; fl2.speed = 11f; fl2.amount = 0.2f;
            HegraKit.MeshObject("FirePool", g, HegraKit.GroundQuad(), M.glowFire, new Vector3(0, 0.03f, 0), null, new Vector3(6f, 1f, 6f))
                .GetComponent<Renderer>().sharedMaterial = M.glowPool;

            // embers: tiny, low-count particle system
            var psGo = new GameObject("Sparks");
            psGo.transform.SetParent(g, false);
            psGo.transform.localPosition = new Vector3(0, 0.3f, 0);
            var ps = psGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1.6f;
            main.startSpeed = 0.6f;
            main.startSize = 0.025f;
            main.maxParticles = 24;
            main.startColor = new Color(1f, 0.6f, 0.25f);
            main.gravityModifier = -0.05f;
            var em = ps.emission;
            em.rateOverTime = 8f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = 0.15f;
            shape.angle = 12f;
            psGo.GetComponent<ParticleSystemRenderer>().sharedMaterial = M.glowFire;

            if (audio != null) Loop3D(g, "FireSound", audio.Get("amb_fire"), audio.ambienceGroup, 0.8f, 10f);
        }

        static AudioSource Loop3D(Transform parent, string name, AudioClip clip, UnityEngine.Audio.AudioMixerGroup group, float volume, float maxDistance)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.up * 0.5f;
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.playOnAwake = true;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 1f;
            src.maxDistance = maxDistance;
            src.volume = volume;
            src.dopplerLevel = 0f;
            src.outputAudioMixerGroup = group;
            return src;
        }

        static void Crate(Transform parent, Vector3 p, Vector3 size, float yaw = 0f, bool collider = true)
        {
            var c = HegraKit.Prim(PrimitiveType.Cube, parent, "Crate", p + Vector3.up * size.y * 0.5f, size, M.wood, new Vector3(0, yaw, 0), collider);
            HegraKit.Prim(PrimitiveType.Cube, c.transform, "Band", Vector3.zero, new Vector3(1.02f, 0.12f, 1.02f), M.rope);
        }

        static void Sack(Transform parent, Vector3 p, float s = 1f) =>
            HegraKit.Prim(PrimitiveType.Sphere, parent, "Sack", p + Vector3.up * 0.22f * s, new Vector3(0.45f, 0.5f, 0.4f) * s, M.earth);

        static void Amphora(Transform parent, Vector3 p, float s = 1f)
        {
            var g = HegraKit.Group("Amphora", parent, p).transform;
            HegraKit.Prim(PrimitiveType.Sphere, g, "Belly", new Vector3(0, 0.3f, 0) * s, new Vector3(0.32f, 0.45f, 0.32f) * s, M.pottery);
            HegraKit.Prim(PrimitiveType.Cylinder, g, "Neck", new Vector3(0, 0.6f, 0) * s, new Vector3(0.12f, 0.1f, 0.12f) * s, M.pottery);
            HegraKit.Prim(PrimitiveType.Cylinder, g, "Lip", new Vector3(0, 0.7f, 0) * s, new Vector3(0.17f, 0.02f, 0.17f) * s, M.pottery);
        }

        static void Tent(Transform parent, Vector3 p, float yaw, float w = 3f, float h = 2.2f, float l = 4f)
        {
            var g = HegraKit.Group("Tent", parent, p, yaw).transform;
            HegraKit.MeshObject("Cloth", g, HegraKit.Prism("TentPrism", 1f, 1f, 1f), M.tent, Vector3.zero, null, new Vector3(w, h, l), true);
            HegraKit.Prim(PrimitiveType.Cube, g, "Stripe", new Vector3(0, h * 0.5f, 0), new Vector3(w * 0.52f, 0.12f, l * 1.01f), M.tentStripe, new Vector3(0, 0, 0));
            HegraKit.Prim(PrimitiveType.Cylinder, g, "PoleFront", new Vector3(0, h * 0.5f, l * 0.5f), new Vector3(0.06f, h * 0.5f, 0.06f), M.wood);
            HegraKit.Prim(PrimitiveType.Cylinder, g, "PoleBack", new Vector3(0, h * 0.5f, -l * 0.5f), new Vector3(0.06f, h * 0.5f, 0.06f), M.wood);
            HegraKit.Prim(PrimitiveType.Cube, g, "Rug", new Vector3(0, 0.01f, l * 0.5f + 0.7f), new Vector3(1.4f, 0.02f, 1f), M.clothRed);
        }

        static Transform Point(Transform parent, string name, Vector3 localPos, Vector3 lookAtLocal)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            var d = lookAtLocal - localPos; d.y = 0;
            if (d.sqrMagnitude > 0.001f) t.localRotation = Quaternion.LookRotation(d);
            return t;
        }

        static TextMeshPro WorldText(Transform parent, string name, Vector3 localPos, float size, string key, Color color, float width = 2f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var t = go.AddComponent<TextMeshPro>();
            t.font = s_Font;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.rectTransform.sizeDelta = new Vector2(width, 1f);
            t.textWrappingMode = TextWrappingModes.Normal;
            if (!string.IsNullOrEmpty(key)) go.AddComponent<LocalizedText>().SetKey(key, TextAlignmentOptions.Center);
            return t;
        }

        static float YawTo(Vector3 from, Vector3 to)
        {
            var d = to - from;
            return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        }

        // ───────────── Stop 1: Caravan Camp ─────────────

        static CampStop BuildCamp(Transform world, HegraAudioAssets.Set audio)
        {
            var root = HegraKit.Group("Stop1_CaravanCamp", world, CampPos).transform;
            var stop = root.gameObject.AddComponent<CampStop>();
            stop.stopId = StopId.Camp;

            var spawnLocal = new Vector3(0, 0, -1.5f);
            stop.spawnPoint = Point(root, "SpawnPoint", spawnLocal, new Vector3(0, 0, 5));
            // Shuʿayd's coin table stands to the right (toward the right tent), clear of the main menu that opens
            // straight ahead of the spawn point; Kamkam waits just beside it
            var tablePos = new Vector3(1.4f, 0, -0.1f);
            var toSpawn = (spawnLocal - tablePos); toSpawn.y = 0; toSpawn.Normalize();
            var tableRight = Vector3.Cross(Vector3.up, -toSpawn);   // the player's right when facing the table
            stop.guidePoint = Point(root, "GuidePoint", tablePos + tableRight * 0.85f + toSpawn * 0.15f, spawnLocal);

            var decor = HegraKit.Group("Decor", root, Vector3.zero).transform;
            Tent(decor, new Vector3(-5.5f, 0, 3f), 20f);
            Tent(decor, new Vector3(5f, 0, 5f), -30f, 3.4f, 2.4f, 4.5f);
            Tent(decor, new Vector3(-4.5f, 0, -5f), 60f);
            Campfire(root, new Vector3(-1.6f, 0, 1.6f), audio);
            Crate(decor, new Vector3(-3.6f, 0, 0.6f), new Vector3(0.7f, 0.5f, 0.5f), 15f);
            Crate(decor, new Vector3(-3.5f, 0.5f, 0.55f), new Vector3(0.5f, 0.4f, 0.45f), 30f);
            Crate(decor, new Vector3(3.2f, 0, 2.2f), new Vector3(0.8f, 0.6f, 0.6f), -20f);
            Sack(decor, new Vector3(2.6f, 0, 2.6f)); Sack(decor, new Vector3(2.9f, 0, 3.1f), 0.9f); Sack(decor, new Vector3(-3f, 0, 1.4f), 1.1f);
            Amphora(decor, new Vector3(3.6f, 0, 1.4f)); Amphora(decor, new Vector3(-2.6f, 0, -2.4f), 0.9f);
            // frankincense bundles on a cloth
            HegraKit.Prim(PrimitiveType.Cube, decor, "IncenseCloth", new Vector3(2.4f, 0.01f, -2.2f), new Vector3(1.2f, 0.02f, 0.9f), M.clothSaffron);
            for (int i = 0; i < 6; i++)
                HegraKit.Prim(PrimitiveType.Sphere, decor, "Resin" + i, new Vector3(2.0f + (i % 3) * 0.3f, 0.06f, -2.4f + (i / 3) * 0.35f), Vector3.one * 0.12f, M.resin);
            HegraKit.MarkStatic(decor.gameObject);

            // Shuʿayd's coin table (he stands just behind it): a bronze plate with the three coins, at chest height so
            // it is in view without looking down. The lamp is no longer here: it starts in the player's hand.
            const float tableTop = 1.12f;
            var coinTable = HegraKit.Group("CoinTable", root, tablePos, YawTo(tablePos, spawnLocal)).transform;
            // dark, matte wood so nothing on the table turns into a bright patch
            var tableWood = HegraKit.Lit("M_LampTableWood", new Color(0.24f, 0.15f, 0.1f), null, 0f);
            HegraKit.Prim(PrimitiveType.Cube, coinTable, "Top", new Vector3(0, tableTop - 0.03f, 0), new Vector3(0.8f, 0.06f, 0.55f), tableWood, null, true);
            foreach (var lx in new[] { -0.33f, 0.33f })
                foreach (var lz in new[] { -0.22f, 0.22f })
                    HegraKit.Prim(PrimitiveType.Cube, coinTable, "Leg", new Vector3(lx, (tableTop - 0.06f) * 0.5f, lz), new Vector3(0.06f, tableTop - 0.06f, 0.06f), M.wood);

            // the plate is the interactable (point + trigger/grip, or reach and grip); a generous box makes it easy
            var plate = HegraKit.Prim(PrimitiveType.Cylinder, coinTable, "CoinPlate", new Vector3(0, tableTop + 0.01f, 0), new Vector3(0.26f, 0.01f, 0.26f), M.bronze);
            var plateBox = plate.AddComponent<BoxCollider>();
            plateBox.size = new Vector3(1.4f, 10f, 1.4f);
            plateBox.center = new Vector3(0f, 4f, 0f);
            stop.coinPlate = plate.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            var pile = HegraKit.Group("Coins", coinTable, new Vector3(0, tableTop + 0.025f, 0)).transform;
            HegraKit.Prim(PrimitiveType.Cylinder, pile, "Coin1", new Vector3(-0.035f, 0f, 0.01f), new Vector3(0.045f, 0.004f, 0.045f), M.coinGold);
            HegraKit.Prim(PrimitiveType.Cylinder, pile, "Coin2", new Vector3(0.03f, 0f, -0.02f), new Vector3(0.045f, 0.004f, 0.045f), M.coinGold);
            HegraKit.Prim(PrimitiveType.Cylinder, pile, "Coin3", new Vector3(0.005f, 0.008f, 0.025f), new Vector3(0.045f, 0.004f, 0.045f), M.coinGold, new Vector3(8f, 0f, 5f));
            stop.coinPile = pile.gameObject;

            // a small bobbing golden arrow above the coins, only once a journey has started and until they are taken
            var beacon = coinTable.gameObject.AddComponent<CampBeacon>();
            var gold = HegraKit.Unlit("M_BeaconGold", new Color(1f, 0.78f, 0.3f));
            var arrow = HegraKit.Group("CoinArrow", coinTable, new Vector3(0, tableTop + 0.3f, 0)).transform;
            arrow.localScale = Vector3.one * 0.6f;
            HegraKit.MeshObject("Head", arrow, HegraKit.Prism("BeaconArrowHead", 0.14f, 0.12f, 0.03f), gold, Vector3.zero, new Vector3(0, 0, 180f));
            HegraKit.Prim(PrimitiveType.Cube, arrow, "Shaft", new Vector3(0, 0.08f, 0), new Vector3(0.045f, 0.11f, 0.02f), gold);
            beacon.arrow = arrow;
            arrow.gameObject.SetActive(false);

            // the objective card ("take the coins…") is pinned just left of the table (toward the middle of the view)
            var cardPos = tablePos - tableRight * 0.45f + toSpawn * 0.25f + Vector3.up * 1.42f;
            stop.coinCard = Point(root, "CoinCard", cardPos, cardPos + (cardPos - (spawnLocal + Vector3.up * 1.4f)));

            // where the lamp prefab is placed in the scene; at runtime it goes straight into the player's hand
            Point(root, "LampSpot", spawnLocal + new Vector3(0.2f, 1.0f, 0.3f), spawnLocal + new Vector3(0.2f, 1.0f, 2f));

            // glowing rug used by the teleport lesson
            var rug = HegraKit.Group("TeleportRug", root, new Vector3(-1.4f, 0, 4.6f)).transform;
            HegraKit.Prim(PrimitiveType.Cube, rug, "Rug", new Vector3(0, 0.01f, 0), new Vector3(1.5f, 0.02f, 1f), M.clothRed);
            var marker = HegraKit.MeshObject("Marker", rug, HegraKit.GroundQuad(), M.glowMarker, new Vector3(0, 0.03f, 0), null, new Vector3(2.4f, 1f, 2.4f));
            stop.teleportMarker = rug;
            stop.markerVisual = marker;

            // characters
            var shuaydPos = new Vector3(1.7f, 0, 0.6f);
            var f = HegraCharacters.Build("Shuayd", root, shuaydPos, YawTo(shuaydPos, spawnLocal), M.earth, M.skinB, 1.8f);
            HegraCharacters.Headcloth(f, M.earth);
            HegraCharacters.Belt(f, M.leather);
            HegraCharacters.Spear(f, M.wood, M.bronze);
            HegraCharacters.Shield(f, M.leather, M.bronze);
            f.npc.speakerId = "shuayd";
            stop.shuayd = f.npc;

            var camel = HegraCharacters.Camel(root, new Vector3(5f, 0, -1.5f), 200f, M);
            stop.camel = camel.transform;
            HegraCharacters.Camel(root, new Vector3(7f, 0, 1.5f), 160f, M).name = "Camel2";

            stop.marketDirection = Point(root, "ToMarket", new Vector3(0, 1.5f, 18f), Vector3.zero);
            Lantern(root, new Vector3(1.2f, 0, 6.5f));
            Lantern(root, new Vector3(-2.4f, 0, -3.2f), 1.6f);

            var zone = root.gameObject.AddComponent<AmbienceZone>();
            zone.stop = StopId.Camp;
            zone.radius = 12f;
            zone.music = null;

            var card = EduCardObject(root, new Vector3(-2.8f, 0, -1.2f), "edu.camp");
            return stop;
        }

        // ───────────── educational card stand ─────────────

        static GameObject EduCardObject(Transform parent, Vector3 localPos, string keyPrefix)
        {
            var g = HegraKit.Group("EduCard_" + keyPrefix, parent, localPos).transform;
            HegraKit.Prim(PrimitiveType.Cylinder, g, "Stand", new Vector3(0, 0.55f, 0), new Vector3(0.05f, 0.55f, 0.05f), M.wood);
            var board = HegraKit.Prim(PrimitiveType.Cube, g, "Board", new Vector3(0, 1.25f, 0), new Vector3(0.9f, 0.6f, 0.03f), M.linen, new Vector3(-15, 0, 0));
            var title = WorldText(board.transform, "Title", new Vector3(0, 0.33f, -0.6f), 1.6f, keyPrefix + ".title", HegraPalette.SandstoneShadow, 0.95f);
            title.transform.localScale = new Vector3(1f / 0.9f, 1f / 0.6f, 1f) * 0.9f;
            var body = WorldText(board.transform, "Body", new Vector3(0, -0.1f, -0.6f), 1.05f, keyPrefix + ".body", new Color(0.2f, 0.14f, 0.12f), 0.95f);
            body.transform.localScale = new Vector3(1f / 0.9f, 1f / 0.6f, 1f) * 0.9f;
            body.rectTransform.sizeDelta = new Vector2(0.95f, 0.55f);
            var card = g.gameObject.AddComponent<EduCard>();
            card.titleKey = keyPrefix + ".title";
            card.bodyKey = keyPrefix + ".body";
            return g.gameObject;
        }

        // ───────────── remaining stops: anchors, zones and route lanterns ─────────────

        // computed (not a static field) so it never sees uninitialized positions from the other partial file
        static (StopId id, string name, Vector3 pos)[] s_StopDefs => new[]
        {
            (StopId.Market, "Stop2_Market", MarketPos),
            (StopId.Theater, "Stop3_TheaterSquare", TheaterPos),
            (StopId.Ithlib, "Stop4_JabalIthlib", IthlibPos),
            (StopId.Bint, "Stop5_QasrAlBint", BintPos),
            (StopId.Farid, "Stop6_QasrAlFarid", FaridPos),
        };

        static void BuildStopPlaceholders(Transform world, HegraAudioAssets.Set audio, List<StopBase> stops)
        {
            // route lanterns between consecutive stops
            var route = HegraKit.Group("RouteLanterns", world, Vector3.zero).transform;
            var pts = new List<Vector3> { CampPos };
            foreach (var d in s_StopDefs) pts.Add(d.pos);
            for (int i = 0; i < pts.Count - 1; i++)
            {
                var a = pts[i]; var b = pts[i + 1];
                var dir = (b - a); dir.y = 0;
                float len = dir.magnitude;
                var side = Vector3.Cross(Vector3.up, dir.normalized);
                for (float t = 7f; t < len - 7f; t += 7f)
                    Lantern(route, a + dir.normalized * t + side * ((int)(t / 7f) % 2 == 0 ? 1.8f : -1.8f), 1.8f);
            }

            foreach (var d in s_StopDefs)
            {
                if (stops.Exists(st => st && st.stopId == d.id)) continue; // built by a dedicated builder
                var root = HegraKit.Group(d.name, world, d.pos).transform;
                var prev = pts[pts.IndexOf(d.pos) - 1];
                var approach = (prev - d.pos); approach.y = 0;
                var spawnLocal = approach.normalized * 5f;
                Point(root, "SpawnPoint", spawnLocal, Vector3.zero);
                Point(root, "GuidePoint", approach.normalized * 2.5f + Vector3.Cross(Vector3.up, approach.normalized) * 1.5f, spawnLocal);
                var zone = root.gameObject.AddComponent<AmbienceZone>();
                zone.stop = d.id;
                zone.radius = 14f;
                switch (d.id)
                {
                    // music is the single journey track (AudioDirector); zones only carry ambience
                    case StopId.Market: zone.ambience = audio.Get("amb_market"); zone.ambienceVolume = 0.55f; break;
                    case StopId.Ithlib: zone.ambience = audio.Get("amb_water"); zone.ambienceVolume = 0.5f; break;
                }
                Lantern(root, new Vector3(2f, 0, 0));
                Lantern(root, new Vector3(-2f, 0, 0));
            }
        }

        static KamkamGuide BuildKamkam(Transform world)
        {
            var pos = MarketPos + new Vector3(2.5f, 0, -1f);
            var f = HegraCharacters.Build("Kamkam", world, pos, 180f, M.violet, M.skinA, 1.66f);
            HegraCharacters.Veil(f, M.kohlBlack);
            HegraCharacters.Jewelry(f, M.bronze);
            HegraCharacters.Belt(f, M.bronze, 1.0f);
            f.npc.speakerId = "kamkam";
            var guide = f.root.AddComponent<KamkamGuide>();
            guide.figure = f.npc;
            return guide;
        }
    }
}
