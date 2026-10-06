using System.IO;
using TMPro;
using UnityEngine;

namespace SecretOfHegra.EditorTools
{
    public static partial class HegraBuilder
    {
        static NabataeanConverter s_Nab;

        static string NabataeanDisplay(string word)
        {
            s_Nab ??= NabataeanConverter.FromJson(File.ReadAllText(HegraKit.Root + "/Data/nabataean_map.json"));
            return s_Nab.Convert(word).displayText;
        }

        /// <summary>Crow-step crown (the stepped motif topping many Hegra facades), centered at local origin.</summary>
        static void CrowSteps(Transform parent, Vector3 local, float width, float stepH, Material mat, int steps = 4)
        {
            var g = HegraKit.Group("CrowSteps", parent, local).transform;
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < steps; i++)
                {
                    float w = width * 0.22f * (1f - i * 0.18f);
                    float x = side * (width * 0.32f - i * width * 0.05f);
                    HegraKit.Prim(PrimitiveType.Cube, g, "Step", new Vector3(x, stepH * (i + 0.5f), 0), new Vector3(w, stepH, 0.5f), mat);
                }
        }

        static Transform EagleShape(Transform parent, string name, Vector3 local, float s, Material mat)
        {
            var g = HegraKit.Group(name, parent, local).transform;
            HegraKit.Prim(PrimitiveType.Capsule, g, "Body", Vector3.zero, new Vector3(0.18f, 0.32f, 0.1f) * s, mat);
            HegraKit.Prim(PrimitiveType.Sphere, g, "Head", new Vector3(0, 0.36f, 0) * s, new Vector3(0.14f, 0.14f, 0.12f) * s, mat);
            HegraKit.Prim(PrimitiveType.Cube, g, "Beak", new Vector3(0.07f, 0.34f, 0) * s, new Vector3(0.08f, 0.04f, 0.05f) * s, mat);
            HegraKit.Prim(PrimitiveType.Cube, g, "WingL", new Vector3(-0.32f, 0.12f, 0) * s, new Vector3(0.5f, 0.18f, 0.05f) * s, mat, new Vector3(0, 0, 18f));
            HegraKit.Prim(PrimitiveType.Cube, g, "WingR", new Vector3(0.32f, 0.12f, 0) * s, new Vector3(0.5f, 0.18f, 0.05f) * s, mat, new Vector3(0, 0, -18f));
            HegraKit.Prim(PrimitiveType.Cube, g, "Tail", new Vector3(0, -0.36f, 0) * s, new Vector3(0.16f, 0.16f, 0.05f) * s, mat);
            return g;
        }

        static TextMeshPro CarvedText(Transform parent, string name, Vector3 local, string displayText, float size, Color color, TMP_FontAsset font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            var t = go.AddComponent<TextMeshPro>();
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.isRightToLeftText = true;
            t.rectTransform.sizeDelta = new Vector2(1.6f, 0.4f);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.text = displayText;
            return t;
        }

        // ───────────── Stop 3: Theater Square ─────────────

        static TheaterStop BuildTheater(Transform world, HegraAudioAssets.Set audio)
        {
            float yaw = YawTo(MarketPos, TheaterPos);
            var root = HegraKit.Group("Stop3_TheaterSquare", world, TheaterPos, yaw).transform;
            var stop = root.gameObject.AddComponent<TheaterStop>();
            stop.stopId = StopId.Theater;
            stop.spawnPoint = Point(root, "SpawnPoint", new Vector3(0, 0, -8.5f), new Vector3(0, 0, 5f));
            stop.guidePoint = Point(root, "GuidePoint", new Vector3(-2.2f, 0, -1.2f), new Vector3(0, 0, -8f));

            var decor = HegraKit.Group("Decor", root, Vector3.zero).transform;
            HegraKit.Prim(PrimitiveType.Cylinder, decor, "Orchestra", new Vector3(0, 0.005f, 1f), new Vector3(9f, 0.005f, 9f), M.sandstoneShadow);

            // three tiers of stone seats on an arc, with a central aisle
            var center = new Vector3(0, 0, 3f);
            for (int tier = 0; tier < 3; tier++)
            {
                float r = 5.4f + tier * 1.05f;
                float h = 0.38f * (tier + 1);
                for (float a = 128f; a <= 232f; a += 13f)
                {
                    if (Mathf.Abs(a - 180f) < 8f) continue;
                    var dir = new Vector3(Mathf.Sin(a * Mathf.Deg2Rad), 0, Mathf.Cos(a * Mathf.Deg2Rad));
                    var p = center + dir * r;
                    HegraKit.Prim(PrimitiveType.Cube, decor, "Seat", p + Vector3.up * h * 0.5f, new Vector3(1.25f, h, 0.9f), M.sandstoneLit,
                        new Vector3(0, a + 180f, 0), true);
                }
            }

            // stage + backdrop facade with crow steps
            HegraKit.Prim(PrimitiveType.Cube, decor, "Stage", new Vector3(0, 0.3f, 4.8f), new Vector3(8.5f, 0.6f, 3.6f), M.sandstoneLit, null, true);
            HegraKit.Prim(PrimitiveType.Cube, decor, "StageStep", new Vector3(0, 0.15f, 2.85f), new Vector3(3f, 0.3f, 0.5f), M.sandstoneShadow, null, true);
            HegraKit.Prim(PrimitiveType.Cube, decor, "Backdrop", new Vector3(0, 2.4f, 6.9f), new Vector3(8.5f, 4.2f, 0.6f), M.sandstoneLit, null, true);
            HegraKit.Prim(PrimitiveType.Cube, decor, "Cornice", new Vector3(0, 4.45f, 6.75f), new Vector3(8.8f, 0.25f, 0.8f), M.sandstoneShadow);
            CrowSteps(decor, new Vector3(0, 4.55f, 6.85f), 8.5f, 0.32f, M.sandstoneLit);
            foreach (var x in new[] { -3.4f, 3.4f })
                HegraKit.Prim(PrimitiveType.Cube, decor, "Pilaster", new Vector3(x, 2.4f, 6.55f), new Vector3(0.45f, 4.1f, 0.2f), M.sandstoneShadow);
            HegraKit.Prim(PrimitiveType.Cube, decor, "Curtain", new Vector3(0, 2.0f, 6.58f), new Vector3(4.6f, 2.8f, 0.05f), M.indigo);
            HegraKit.MarkStatic(decor.gameObject);
            HegraKit.MeshObject("StageGlow", root, HegraKit.GroundQuad(), M.glowPool, new Vector3(0, 0.62f, 4.8f), null, new Vector3(7f, 1f, 3.4f));
            Lantern(root, new Vector3(-4.0f, 0, 2.9f), 2.3f);
            Lantern(root, new Vector3(4.0f, 0, 2.9f), 2.3f);
            Lantern(root, new Vector3(-6.8f, 0, -1f));
            Lantern(root, new Vector3(6.8f, 0, -1f));
            stop.stageCenter = Point(root, "StageCenter", new Vector3(0, 1.2f, 4.5f), new Vector3(0, 1.2f, 0));

            // Maliku: white tunic with blue stripes, red mantle, lyre
            var mf = HegraCharacters.Build("Maliku", root, new Vector3(2.9f, 0.6f, 3.9f), 180f, M.white, M.skinA, 1.76f);
            HegraCharacters.Stripes(mf, M.stripeBlue);
            HegraCharacters.Mantle(mf, M.mantleRed);
            HegraCharacters.Headband(mf, M.stripeBlue);
            HegraCharacters.Lyre(mf, M.wood, M.bronze);
            mf.npc.speakerId = "maliku";
            stop.maliku = mf.npc;

            // the play's props
            var props = HegraKit.Group("Performance", root, Vector3.zero).transform;
            var dirc = props.gameObject.AddComponent<PerformanceDirector>();
            dirc.maliku = mf.npc;
            var mason = HegraCharacters.Build("MasonPuppet", props, new Vector3(-3.1f, 0.6f, 5.1f), 90f, M.earth, M.skinB, 1.3f);
            HegraCharacters.Headband(mason, M.linen);
            HegraCharacters.Apron(mason, M.leather);
            mason.npc.facePlayer = false;
            Object.DestroyImmediate(mason.root.GetComponent<CapsuleCollider>());
            dirc.mason = mason.npc;
            dirc.masonStart = Point(props, "MasonStart", new Vector3(-3.1f, 0.6f, 5.1f), new Vector3(0, 0.6f, 5.1f));
            dirc.masonAtRock = Point(props, "MasonAtRock", new Vector3(0.6f, 0.6f, 5.3f), new Vector3(2f, 0.6f, 5.4f));
            dirc.masonExit = Point(props, "MasonExit", new Vector3(-4.6f, 0.6f, 5.8f), new Vector3(-6f, 0.6f, 5.8f));
            HegraKit.MeshObject("StageRock", props, HegraKit.Rock("StageRock", 9, 14, 10, 0.18f, 0.6f), M.rough, new Vector3(1.8f, 1.0f, 5.5f), new Vector3(0, 30, 0), new Vector3(0.8f, 0.7f, 0.7f));
            var crown = HegraKit.Group("RockCrown", props, new Vector3(1.8f, 1.65f, 5.5f)).transform;
            HegraKit.Prim(PrimitiveType.Cube, crown, "CrownL", new Vector3(-0.22f, 0.12f, 0), new Vector3(0.2f, 0.24f, 0.3f), M.sandstoneLit);
            HegraKit.Prim(PrimitiveType.Cube, crown, "CrownR", new Vector3(0.22f, 0.12f, 0), new Vector3(0.2f, 0.24f, 0.3f), M.sandstoneLit);
            HegraKit.Prim(PrimitiveType.Cube, crown, "CrownM", new Vector3(0, 0.06f, 0), new Vector3(0.7f, 0.12f, 0.3f), M.sandstoneLit);
            dirc.crown = crown;
            var moon = HegraKit.Prim(PrimitiveType.Sphere, props, "StageMoon", new Vector3(-2.2f, 1.0f, 6.3f), Vector3.one * 0.55f, M.moon);
            HegraKit.Prim(PrimitiveType.Quad, moon.transform, "Halo", Vector3.zero, Vector3.one * 3f, M.glowMoon).AddComponent<FlameFlicker>().billboardYaw = true;
            HegraKit.Prim(PrimitiveType.Cylinder, props, "MoonPole", new Vector3(-2.2f, 1.6f, 6.5f), new Vector3(0.03f, 1.0f, 0.03f), M.wood);
            dirc.moon = moon.transform;
            dirc.moonLow = Point(props, "MoonLow", new Vector3(-2.2f, 1.0f, 6.3f), Vector3.zero);
            dirc.moonHigh = Point(props, "MoonHigh", new Vector3(-2.2f, 3.5f, 6.3f), Vector3.zero);
            var chest = HegraKit.Group("PropChest", props, new Vector3(1.0f, 0.78f, 4.9f)).transform;
            HegraKit.Prim(PrimitiveType.Cube, chest, "Box", Vector3.zero, new Vector3(0.6f, 0.36f, 0.4f), M.wood);
            HegraKit.Prim(PrimitiveType.Cube, chest, "Band", Vector3.zero, new Vector3(0.62f, 0.06f, 0.42f), M.bronze);
            dirc.chest = chest;
            var tokens = HegraKit.Group("SignTokens", props, new Vector3(0, 2.7f, 5.9f)).transform;
            var tab = HegraKit.Group("TokenInscription", tokens, new Vector3(-1.6f, 0, 0)).transform;
            HegraKit.Prim(PrimitiveType.Cube, tab, "Tablet", Vector3.zero, new Vector3(0.5f, 0.36f, 0.06f), M.sandstoneLit);
            HegraKit.Prim(PrimitiveType.Quad, tab, "Glow", new Vector3(0, 0, -0.05f), Vector3.one * 0.9f, M.glowWarm);
            var water = HegraKit.Group("TokenWater", tokens, Vector3.zero).transform;
            HegraKit.Prim(PrimitiveType.Sphere, water, "Drop", Vector3.zero, new Vector3(0.28f, 0.38f, 0.28f), M.water);
            HegraKit.Prim(PrimitiveType.Quad, water, "Glow", new Vector3(0, 0, -0.1f), Vector3.one * 0.9f, M.glowBlue);
            var eagle = EagleShape(tokens, "TokenEagle", new Vector3(1.6f, 0, 0), 0.9f, M.bronze);
            HegraKit.Prim(PrimitiveType.Quad, eagle, "Glow", new Vector3(0, 0, -0.1f), Vector3.one * 1.0f, M.glowWarm);
            dirc.tokenInscription = tab; dirc.tokenWater = water; dirc.tokenEagle = eagle;

            // the old wall behind the seats is scenery now; Sign 1 moved to the Elephant Rock beyond the stage
            var wallPos = new Vector3(-6.4f, 0, -5.6f);
            var wallRoot = Point(root, "OldWall", wallPos, wallPos + (wallPos - center));
            HegraKit.Prim(PrimitiveType.Cube, wallRoot, "Wall", new Vector3(0, 1.3f, 0.3f), new Vector3(3.4f, 2.6f, 0.6f), M.sandstoneShadow, null, true);
            HegraKit.Prim(PrimitiveType.Cube, wallRoot, "BrokenTop", new Vector3(0.9f, 2.75f, 0.3f), new Vector3(1.3f, 0.35f, 0.55f), M.sandstoneShadow, new Vector3(0, 0, 8f));

            var rock = BuildElephantRock(root, center);
            stop.rock = rock;
            stop.inscription = rock.reveal;
            stop.director = dirc;

            var beam = HegraKit.Prim(PrimitiveType.Quad, props, "Moonbeam", Vector3.zero, Vector3.one, M.glowMoon);
            beam.SetActive(false);
            dirc.moonbeam = beam.transform;
            dirc.beamTarget = rock.face;

            var zone = root.gameObject.AddComponent<AmbienceZone>();
            zone.stop = StopId.Theater;
            zone.radius = 14f;
            zone.music = null; // journey music plays throughout (AudioDirector)

            EduCardObject(root, new Vector3(5.2f, 0, -4.5f), "edu.theater");
            return stop;
        }
    }
}
