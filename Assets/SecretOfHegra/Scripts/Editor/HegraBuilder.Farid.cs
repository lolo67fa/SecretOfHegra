using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SecretOfHegra.EditorTools
{
    public static partial class HegraBuilder
    {
        // ───────────── Stop 6: Qasr al-Farid + finale ─────────────

        static FaridStop BuildFarid(Transform world, HegraAudioAssets.Set audio)
        {
            float yaw = YawTo(BintPos, FaridPos);
            var root = HegraKit.Group("Stop6_QasrAlFarid", world, FaridPos, yaw).transform;
            var stop = root.gameObject.AddComponent<FaridStop>();
            stop.stopId = StopId.Farid;
            stop.spawnPoint = Point(root, "SpawnPoint", new Vector3(0, 0, -8f), new Vector3(0, 0, 5f));
            stop.guidePoint = Point(root, "GuidePoint", new Vector3(-2.4f, 0, -3f), new Vector3(0, 0, -8f));
            stop.facadeCenter = Point(root, "FacadeCenter", new Vector3(0, 2f, -0.6f), new Vector3(0, 2f, -6f));

            // the lonely rock (visual only; the chamber inside it has its own collidable walls)
            var decor = HegraKit.Group("Decor", root, Vector3.zero).transform;
            // built as masses around the chamber (x ±2.7, z 0–6.3, y 0–3.4) so no rock surface intrudes into it
            var rockMesh = RockMeshes("FaridRock", 61, 20, 12, 0.2f, 0.2f);
            RockObject("RockLeft", decor, rockMesh, new Vector3(-6.8f, 0, 5.6f), new Vector3(0, 15, 0), new Vector3(3.2f, 9f, 5.6f), false);
            RockObject("RockRight", decor, rockMesh, new Vector3(6.8f, 0, 5.6f), new Vector3(0, 195, 0), new Vector3(3.2f, 8.6f, 5.6f), false);
            RockObject("RockBack", decor, rockMesh, new Vector3(0, 0, 12.6f), new Vector3(0, 40, 0), new Vector3(6.8f, 9.5f, 4.5f), false);
            RockObject("RockTop", decor, rockMesh, new Vector3(0, 8.6f, 5.4f), new Vector3(0, 70, 0), new Vector3(6.4f, 4f, 5.2f), false);

            // facade: finished, decorated upper part; rough, unfinished lower part (as at the real tomb)
            var face = HegraKit.Group("Facade", decor, new Vector3(0, 0, -0.4f)).transform;
            HegraKit.Prim(PrimitiveType.Cube, face, "FaceLeft", new Vector3(-2.45f, 4.25f, 1.0f), new Vector3(3.3f, 8.5f, 2.6f), M.sandstoneLit, null, true);
            HegraKit.Prim(PrimitiveType.Cube, face, "FaceRight", new Vector3(2.45f, 4.25f, 1.0f), new Vector3(3.3f, 8.5f, 2.6f), M.sandstoneLit, null, true);
            HegraKit.Prim(PrimitiveType.Cube, face, "FaceTop", new Vector3(0, 5.65f, 1.0f), new Vector3(1.6f, 5.7f, 2.6f), M.sandstoneLit, null, true);
            foreach (var x in new[] { -2.7f, -1.1f, 1.1f, 2.7f })
                HegraKit.Prim(PrimitiveType.Cube, face, "Pilaster", new Vector3(x, 5.3f, -0.36f), new Vector3(0.36f, 4.6f, 0.16f), M.sandstoneShadow);
            HegraKit.Prim(PrimitiveType.Cube, face, "Cornice", new Vector3(0, 7.75f, -0.4f), new Vector3(6.2f, 0.22f, 0.3f), M.sandstoneShadow);
            CrowSteps(face, new Vector3(0, 8.5f, 0), 6f, 0.4f, M.sandstoneLit);
            // unfinished band: rough chisel blocks across the lower facade
            var rough = HegraKit.Group("RoughBand", face, Vector3.zero).transform;
            var rnd = new System.Random(6);
            for (float x = -2.9f; x <= 2.9f; x += 0.55f)
            {
                if (Mathf.Abs(x) < 0.95f || Mathf.Abs(x - 1.55f) < 0.4f) continue; // doorway and the niche stay clear
                float h = 2.2f + (float)rnd.NextDouble() * 0.6f;
                HegraKit.Prim(PrimitiveType.Cube, rough, "Rough", new Vector3(x, h * 0.5f, -0.38f - (float)rnd.NextDouble() * 0.12f),
                    new Vector3(0.6f, h, 0.24f), M.rough, new Vector3(0, (float)rnd.NextDouble() * 10f - 5f, (float)rnd.NextDouble() * 6f - 3f));
            }
            HegraKit.Prim(PrimitiveType.Cube, face, "DoorFrame", new Vector3(0, 2.75f, -0.36f), new Vector3(1.9f, 0.16f, 0.14f), M.sandstoneShadow);

            // the stone door, which sinks into the ground
            var door = HegraKit.Prim(PrimitiveType.Cube, root, "StoneDoor", new Vector3(0, 1.32f, -0.45f), new Vector3(1.55f, 2.64f, 0.45f), M.sandstoneLit, null, true);
            stop.door = door.transform;
            stop.doorOpenOffset = new Vector3(0, -2.75f, 0);

            // niche for the name shard, in the rough band to the right of the door
            var nicheRoot = HegraKit.Group("Niche", root, new Vector3(1.55f, 1.2f, -0.78f)).transform;
            HegraKit.Prim(PrimitiveType.Cube, nicheRoot, "Recess", new Vector3(0, 0, 0.06f), new Vector3(0.34f, 0.26f, 0.08f), M.darkInterior);
            HegraKit.Prim(PrimitiveType.Quad, nicheRoot, "NicheGlow", new Vector3(0, 0, -0.02f), new Vector3(0.6f, 0.5f, 1f), M.glowWarm).AddComponent<FlameFlicker>();
            var niche = MakeSocket(nicheRoot, "ShardNiche", new[] { ItemKind.NameShard }, 0.26f);
            niche.transform.localPosition = new Vector3(0, 0, -0.04f);
            niche.attachTransform.localRotation = Quaternion.Euler(0, 180f, 0);
            stop.niche = niche;
            // a ledge under the niche catches a shard that is let go nearby, inside the socket's reach
            HegraKit.Prim(PrimitiveType.Cube, root, "NicheShelf", new Vector3(1.55f, 1.04f, -0.9f), new Vector3(0.5f, 0.04f, 0.26f), M.sandstoneShadow, null, true);

            // the mason's secret chamber
            var ch = HegraKit.Group("SecretChamber", root, new Vector3(0, 0, 3.1f)).transform;
            var wallMat = M.sandstoneShadow;
            HegraKit.Prim(PrimitiveType.Cube, ch, "Floor", new Vector3(0, -0.05f, 0), new Vector3(5.2f, 0.1f, 6.2f), M.sandstoneShadow, null, true);
            HegraKit.Prim(PrimitiveType.Cube, ch, "Ceiling", new Vector3(0, 3.3f, 0), new Vector3(5.2f, 0.2f, 6.2f), wallMat);
            HegraKit.Prim(PrimitiveType.Cube, ch, "WallL", new Vector3(-2.6f, 1.6f, 0), new Vector3(0.2f, 3.3f, 6.2f), wallMat, null, true);
            HegraKit.Prim(PrimitiveType.Cube, ch, "WallR", new Vector3(2.6f, 1.6f, 0), new Vector3(0.2f, 3.3f, 6.2f), wallMat, null, true);
            var back = HegraKit.Prim(PrimitiveType.Cube, ch, "SmoothWall", new Vector3(0, 1.6f, 3.0f), new Vector3(5.2f, 3.3f, 0.2f), M.sandstoneLit, null, true);
            // chamber light and wall lamps, dark until the door opens
            var lightGo = new GameObject("ChamberLight");
            lightGo.transform.SetParent(ch, false);
            lightGo.transform.localPosition = new Vector3(0, 2.7f, 0.4f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.7f, 0.4f);
            light.range = 9f;
            light.intensity = 0f;
            light.shadows = LightShadows.None;
            stop.chamberLight = light;
            stop.chamberLightIntensity = 6f;
            var glows = new List<Renderer>();
            foreach (var x in new[] { -2.45f, 2.45f })
                foreach (var z in new[] { -0.4f, 1.8f })
                {
                    var lamp = HegraKit.Group("WallLamp", ch, new Vector3(x, 1.9f, z), x < 0 ? 90f : -90f).transform;
                    HegraKit.Prim(PrimitiveType.Cube, lamp, "Bracket", new Vector3(0, 0, 0.06f), new Vector3(0.14f, 0.1f, 0.12f), M.bronze);
                    var g = HegraKit.Prim(PrimitiveType.Quad, lamp, "Flame", new Vector3(0, 0.14f, 0.1f), Vector3.one * 0.5f, M.glowFire);
                    g.AddComponent<FlameFlicker>().billboardYaw = true;
                    glows.Add(g.GetComponent<Renderer>());
                }
            stop.chamberGlows = glows.ToArray();

            // the caravan chest: an open-topped, bronze-bound box with an arched lid, heaped with gold
            BuildTreasureChest(stop, ch);

            // the smooth wall that waits for a name
            var nameRoot = HegraKit.Group("NameWall", ch, new Vector3(0, 1.75f, 2.88f)).transform;
            var wallGlow = HegraKit.Prim(PrimitiveType.Quad, nameRoot, "NameGlow", new Vector3(0, -0.1f, 0.01f), new Vector3(3.6f, 1.6f, 1f), M.glowWarm);
            stop.wallGlow = wallGlow.GetComponent<Renderer>();
            var carve = new Color(0.98f, 0.82f, 0.5f);
            stop.wallNabataean = CarvedText(nameRoot, "NameNabataean", new Vector3(0, 0.2f, 0), "", 6f, carve, s_NabFont);
            stop.wallNabataean.rectTransform.sizeDelta = new Vector2(3.6f, 0.7f);
            stop.wallModern = CarvedText(nameRoot, "NameModern", new Vector3(0, -0.45f, 0), "", 4f, HegraPalette.Linen, s_Font);
            stop.wallModern.rectTransform.sizeDelta = new Vector2(3.6f, 0.5f);
            stop.wallFocus = nameRoot;

            // souvenir camera framing the chest and the name wall (rendered on demand only)
            var camGo = new GameObject("SouvenirCamera");
            camGo.transform.SetParent(ch, false);
            camGo.transform.localPosition = new Vector3(0, 1.75f, -1.6f);
            camGo.transform.localRotation = Quaternion.LookRotation(new Vector3(0, 1.45f, 2.9f) - camGo.transform.localPosition);
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 52f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 30f;
            cam.enabled = false;
            cam.stereoTargetEye = StereoTargetEyeMask.None; // an ordinary off-screen camera, not an XR eye
            stop.souvenirCamera = cam;

            stop.keyboardAnchor = Point(root, "KeyboardAnchor", new Vector3(0, 1.4f, -2.4f), new Vector3(0, 1.4f, -6f));
            stop.keyboardAnchor.localRotation *= Quaternion.Euler(12f, 0, 0);

            // warm uplight so the carved facade reads at night (the lanterns alone are emissive only)
            var facadeLightGo = new GameObject("FacadeLight");
            facadeLightGo.transform.SetParent(root, false);
            facadeLightGo.transform.localPosition = new Vector3(0, 1.2f, -3.2f);
            var facadeLight = facadeLightGo.AddComponent<Light>();
            facadeLight.type = LightType.Point;
            facadeLight.color = new Color(1f, 0.72f, 0.45f);
            facadeLight.range = 10f;
            facadeLight.intensity = 4f;
            facadeLight.shadows = LightShadows.None;

            Lantern(root, new Vector3(-2.2f, 0, -2.6f), 1.8f);
            Lantern(root, new Vector3(2.6f, 0, -2.6f), 1.8f);
            Lantern(root, new Vector3(-4.2f, 0, -0.4f), 2.2f);
            Lantern(root, new Vector3(4.2f, 0, -0.4f), 2.2f);
            HegraKit.MarkStatic(decor.gameObject);

            var zone = root.gameObject.AddComponent<AmbienceZone>();
            zone.stop = StopId.Farid;
            zone.radius = 14f;
            zone.music = null; // journey music plays throughout (AudioDirector)
            zone.ambience = audio.Get("amb_wind");
            zone.ambienceVolume = 0.2f;

            EduCardObject(root, new Vector3(-4.4f, 0, -4.4f), "edu.farid");
            EduCardObject(ch, new Vector3(-1.6f, 0, -0.2f), "edu.coins");
            return stop;
        }

        // ───────────── the treasure chest ─────────────

        static void BuildTreasureChest(FaridStop stop, Transform ch)
        {
            const float W = 1.1f, H = 0.55f, D = 0.68f, T = 0.05f;
            var rnd = new System.Random(4207);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

            var gold = HegraKit.Lit("M_TreasureGold", new Color(1f, 0.84f, 0.38f), null, 0.9f, new Color(0.16f, 0.12f, 0.03f));
            var ruby = HegraKit.Lit("M_GemRuby", new Color(0.85f, 0.05f, 0.12f), null, 0.95f, new Color(0.35f, 0f, 0.03f));
            var emerald = HegraKit.Lit("M_GemEmerald", new Color(0.05f, 0.7f, 0.3f), null, 0.95f, new Color(0f, 0.22f, 0.08f));
            var sapphire = HegraKit.Lit("M_GemSapphire", new Color(0.12f, 0.3f, 0.95f), null, 0.95f, new Color(0.02f, 0.07f, 0.32f));
            var pearl = HegraKit.Lit("M_Pearl", new Color(0.96f, 0.94f, 0.88f), null, 0.9f, new Color(0.08f, 0.08f, 0.07f));
            var interior = HegraKit.Lit("M_ChestInterior", new Color(0.18f, 0.1f, 0.06f), null, 0.1f);
            var gems = new[] { ruby, emerald, sapphire };

            var chest = HegraKit.Group("CaravanChest", ch, new Vector3(0, 0, 0.6f)).transform;
            // an invisible solid block keeps the player out of the chest; the visible box is open on top
            var block = HegraKit.Prim(PrimitiveType.Cube, chest, "Body", new Vector3(0, H * 0.5f, 0), new Vector3(W, H, D), M.wood, null, true);
            Object.DestroyImmediate(block.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(block.GetComponent<MeshFilter>());
            HegraKit.Prim(PrimitiveType.Cube, chest, "Floor", new Vector3(0, T * 0.5f, 0), new Vector3(W, T, D), M.wood);
            HegraKit.Prim(PrimitiveType.Cube, chest, "Front", new Vector3(0, H * 0.5f, -D * 0.5f + T * 0.5f), new Vector3(W, H, T), M.wood);
            HegraKit.Prim(PrimitiveType.Cube, chest, "Back", new Vector3(0, H * 0.5f, D * 0.5f - T * 0.5f), new Vector3(W, H, T), M.wood);
            foreach (var sx in new[] { -1f, 1f })
                HegraKit.Prim(PrimitiveType.Cube, chest, "Side", new Vector3(sx * (W * 0.5f - T * 0.5f), H * 0.5f, 0), new Vector3(T, H, D - 2 * T), M.wood);
            // bronze corner posts, feet, bands, rim and a gold lock plate
            foreach (var sx in new[] { -1f, 1f })
                foreach (var sz in new[] { -1f, 1f })
                {
                    HegraKit.Prim(PrimitiveType.Cube, chest, "Corner", new Vector3(sx * W * 0.5f, H * 0.5f, sz * D * 0.5f), new Vector3(0.07f, H + 0.01f, 0.07f), M.bronze);
                    HegraKit.Prim(PrimitiveType.Cube, chest, "Foot", new Vector3(sx * (W * 0.5f - 0.04f), 0.015f, sz * (D * 0.5f - 0.04f)), new Vector3(0.1f, 0.05f, 0.1f), M.bronze);
                }
            // bands and rim are thin strips on the outside only, so the open top stays clear for the gold
            foreach (var bx in new[] { -0.3f, 0.3f })
                foreach (var sz in new[] { -1f, 1f })
                    HegraKit.Prim(PrimitiveType.Cube, chest, "Band", new Vector3(bx, H * 0.5f, sz * (D * 0.5f + 0.004f)), new Vector3(0.06f, H + 0.004f, 0.012f), M.bronze);
            foreach (var sz in new[] { -1f, 1f })
                HegraKit.Prim(PrimitiveType.Cube, chest, "Rim", new Vector3(0, H - 0.025f, sz * (D * 0.5f - T * 0.5f)), new Vector3(W + 0.012f, 0.05f, T + 0.012f), M.bronze);
            foreach (var sx in new[] { -1f, 1f })
                HegraKit.Prim(PrimitiveType.Cube, chest, "Rim", new Vector3(sx * (W * 0.5f - T * 0.5f), H - 0.025f, 0), new Vector3(T + 0.012f, 0.05f, D + 0.012f), M.bronze);
            HegraKit.Prim(PrimitiveType.Cube, chest, "LockPlate", new Vector3(0, H - 0.1f, -D * 0.5f - 0.012f), new Vector3(0.16f, 0.18f, 0.02f), gold);
            HegraKit.Prim(PrimitiveType.Cylinder, chest, "Keyhole", new Vector3(0, H - 0.12f, -D * 0.5f - 0.024f), new Vector3(0.03f, 0.004f, 0.03f), interior, new Vector3(90, 0, 0));

            // the treasure: a heap of gold coins filling the chest, goblets, cut gems, a pearl necklace over the rim,
            // the caravan seal and frankincense, and coins spilled on the floor in front
            var treasure = HegraKit.Group("Treasure", chest, Vector3.zero).transform;
            // a bed of gold filling the chest just below the rim, with a heap rising in the middle
            float rx = W * 0.5f - T - 0.002f, rz = D * 0.5f - T - 0.002f, ry = 0.14f, cy = H - 0.11f, bed = H - 0.07f;
            HegraKit.Prim(PrimitiveType.Cube, treasure, "GoldBed", new Vector3(0, bed - 0.04f, 0), new Vector3(rx * 2f, 0.08f, rz * 2f), gold);
            HegraKit.Prim(PrimitiveType.Sphere, treasure, "GoldHeap", new Vector3(0, cy, 0), new Vector3(rx * 2f, ry * 2f, rz * 2f), gold);
            float HeapY(float px, float pz)
            {
                float k = 1f - (px * px) / (rx * rx) - (pz * pz) / (rz * rz);
                return Mathf.Max(bed, cy + ry * Mathf.Sqrt(Mathf.Max(0f, k)));
            }
            var gemMesh = GemMesh("TreasureGem");
            for (int i = 0; i < 80; i++)
            {
                float x = R(-rx, rx) * 0.93f, z = R(-rz, rz) * 0.9f;
                HegraKit.Prim(PrimitiveType.Cylinder, treasure, "Coin", new Vector3(x, HeapY(x, z) + 0.004f, z),
                    new Vector3(0.055f, 0.004f, 0.055f), gold, new Vector3(R(-25f, 25f), R(0f, 360f), R(-25f, 25f)));
            }
            var goblets = new[] { new Vector3(-0.3f, 0.08f, 8f), new Vector3(0.33f, -0.02f, -14f) };   // x, z, tilt
            for (int i = 0; i < goblets.Length; i++)
            {
                var gp = goblets[i];
                var g = HegraKit.Group("Goblet", treasure, new Vector3(gp.x, HeapY(gp.x, gp.y) - 0.03f, gp.y)).transform;
                g.localEulerAngles = new Vector3(gp.z, R(0f, 360f), 0);
                HegraKit.Prim(PrimitiveType.Cylinder, g, "Base", new Vector3(0, 0.005f, 0), new Vector3(0.08f, 0.005f, 0.08f), gold);
                HegraKit.Prim(PrimitiveType.Cylinder, g, "Stem", new Vector3(0, 0.05f, 0), new Vector3(0.02f, 0.045f, 0.02f), gold);
                HegraKit.Prim(PrimitiveType.Sphere, g, "Bowl", new Vector3(0, 0.12f, 0), new Vector3(0.1f, 0.09f, 0.1f), gold);
                HegraKit.Prim(PrimitiveType.Cylinder, g, "Rim", new Vector3(0, 0.155f, 0), new Vector3(0.1f, 0.012f, 0.1f), gold);
                HegraKit.MeshObject("Gem", g, gemMesh, gems[i % 3], new Vector3(0, 0.12f, -0.05f), new Vector3(90, 0, 0), Vector3.one * 0.022f);
            }
            for (int i = 0; i < 14; i++)
            {
                float x = R(-rx, rx) * 0.75f, z = R(-rz, rz) * 0.75f, size = R(0.03f, 0.05f);
                HegraKit.MeshObject("Gem", treasure, gemMesh, gems[i % 3], new Vector3(x, HeapY(x, z) + size * 0.4f, z),
                    new Vector3(R(-30f, 30f), R(0f, 360f), R(-30f, 30f)), Vector3.one * size);
            }
            // pearl necklace: lies on the gold, then hangs over the front rim
            for (int i = 0; i < 18; i++)
            {
                float u = i / 17f;
                float x = Mathf.Lerp(-0.22f, 0.18f, u);
                float sag = Mathf.Sin(u * Mathf.PI);
                var pos = u < 0.5f
                    ? new Vector3(x, HeapY(x, -rz * 0.6f) + 0.01f, -rz * 0.6f - sag * 0.12f)
                    : new Vector3(x, H + 0.01f - sag * 0.18f, -D * 0.5f - 0.025f);
                HegraKit.Prim(PrimitiveType.Sphere, treasure, "Pearl", pos, Vector3.one * 0.026f, pearl);
            }
            HegraKit.Prim(PrimitiveType.Sphere, treasure, "Pendant", new Vector3(0.06f, H - 0.17f, -D * 0.5f - 0.03f), new Vector3(0.05f, 0.065f, 0.025f), ruby);
            HegraKit.Prim(PrimitiveType.Cylinder, treasure, "CaravanSeal", new Vector3(0.05f, HeapY(0.05f, 0.12f) + 0.01f, 0.12f), new Vector3(0.1f, 0.018f, 0.1f), M.bronze, new Vector3(-10, 0, 6));
            for (int i = 0; i < 5; i++)
            {
                float x = 0.12f + (i % 3) * 0.06f, z = 0.12f + (i / 3) * 0.06f;
                HegraKit.Prim(PrimitiveType.Sphere, treasure, "Frankincense", new Vector3(x, HeapY(x, z) + 0.015f, z), Vector3.one * 0.045f, M.resin);
            }
            for (int i = 0; i < 14; i++)
                HegraKit.Prim(PrimitiveType.Cylinder, treasure, "SpilledCoin", new Vector3(R(-0.45f, 0.45f), 0.004f + (i % 3) * 0.006f, -D * 0.5f - R(0.05f, 0.35f)),
                    new Vector3(0.055f, 0.004f, 0.055f), gold, new Vector3(R(-4f, 4f), R(0f, 360f), R(-4f, 4f)));
            HegraKit.MeshObject("SpilledGem", treasure, gemMesh, emerald, new Vector3(-0.18f, 0.012f, -D * 0.5f - 0.2f), new Vector3(80, 30, 0), Vector3.one * 0.035f);
            HegraKit.MeshObject("SpilledGem", treasure, gemMesh, sapphire, new Vector3(0.26f, 0.012f, -D * 0.5f - 0.12f), new Vector3(80, 110, 0), Vector3.one * 0.03f);
            stop.treasure = treasure.gameObject;

            // a warm glow rises from the gold as the lid opens
            var lightGo = new GameObject("TreasureGlow");
            lightGo.transform.SetParent(chest, false);
            lightGo.transform.localPosition = new Vector3(0, H + 0.3f, -0.15f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.78f, 0.4f);
            light.range = 1.8f;
            light.intensity = 0f;
            light.shadows = LightShadows.None;
            stop.treasureLight = light;

            // arched, bronze-bound lid hinged at the back edge
            var lidPivot = HegraKit.Group("LidPivot", chest, new Vector3(0, H, D * 0.5f)).transform;
            HegraKit.Prim(PrimitiveType.Cube, lidPivot, "Lid", new Vector3(0, 0.03f, -D * 0.5f), new Vector3(W + 0.02f, 0.06f, D + 0.02f), M.wood);
            var arch = ArchMesh("ChestLidArch", W + 0.02f, 0.2f, D * 0.5f + 0.01f, 16);
            HegraKit.MeshObject("LidArch", lidPivot, arch, M.wood, new Vector3(0, 0.06f, -D * 0.5f));
            foreach (var bx in new[] { -0.3f, 0.3f })
                HegraKit.MeshObject("LidBand", lidPivot, arch, M.bronze, new Vector3(bx, 0.061f, -D * 0.5f), null, new Vector3(0.06f / (W + 0.02f), 1.04f, 1.04f));
            foreach (var sx in new[] { -1f, 1f })
                HegraKit.MeshObject("LidEdge", lidPivot, arch, M.bronze, new Vector3(sx * (W * 0.5f + 0.005f), 0.061f, -D * 0.5f), null, new Vector3(0.05f / (W + 0.02f), 1.04f, 1.04f));
            HegraKit.Prim(PrimitiveType.Cube, lidPivot, "LidRim", new Vector3(0, 0.03f, -D * 0.5f), new Vector3(W + 0.035f, 0.05f, D + 0.035f), M.bronze);
            var handle = HegraKit.Prim(PrimitiveType.Cube, lidPivot, "LidHandle", new Vector3(0, 0.03f, -D - 0.035f), new Vector3(0.24f, 0.07f, 0.07f), gold, null, true);
            stop.chestLid = lidPivot;
            stop.lidOpenEuler = new Vector3(105f, 0, 0); // front edge swings up and back
            stop.lidHandle = handle.AddComponent<XRSimpleInteractable>();
        }

        /// <summary>Faceted cut gem (an elongated octahedron, flat-shaded) about one unit across.</summary>
        static Mesh GemMesh(string name)
        {
            var top = new Vector3(0, 0.75f, 0);
            var bottom = new Vector3(0, -0.5f, 0);
            var ring = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                ring[i] = new Vector3(Mathf.Cos(a) * 0.5f, 0, Mathf.Sin(a) * 0.5f);
            }
            var v = new List<Vector3>();
            var t = new List<int>();
            for (int i = 0; i < 6; i++)
            {
                var a = ring[i];
                var b = ring[(i + 1) % 6];
                int c = v.Count;
                v.Add(top); v.Add(b); v.Add(a);
                v.Add(bottom); v.Add(a); v.Add(b);
                t.AddRange(new[] { c, c + 1, c + 2, c + 3, c + 4, c + 5 });
            }
            var mesh = new Mesh();
            mesh.SetVertices(v);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return HegraKit.SaveMesh(mesh, name);
        }

        /// <summary>Half-elliptical barrel along x (width w): base on y=0, crown at height h, half-depth r in z.</summary>
        static Mesh ArchMesh(string name, float w, float h, float r, int segments)
        {
            var v = new List<Vector3>();
            var t = new List<int>();
            float hw = w * 0.5f;
            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.PI * i / segments;
                float y = Mathf.Sin(a) * h, z = Mathf.Cos(a) * r;
                v.Add(new Vector3(-hw, y, z));
                v.Add(new Vector3(hw, y, z));
            }
            for (int i = 0; i < segments; i++)
            {
                int a = i * 2, b = a + 2;
                t.AddRange(new[] { a, a + 1, b + 1, a, b + 1, b });
            }
            foreach (var sx in new[] { -1f, 1f })
            {
                int c = v.Count;
                v.Add(new Vector3(sx * hw, 0, 0));
                for (int i = 0; i <= segments; i++)
                {
                    float a = Mathf.PI * i / segments;
                    v.Add(new Vector3(sx * hw, Mathf.Sin(a) * h, Mathf.Cos(a) * r));
                }
                for (int i = 0; i < segments; i++)
                    t.AddRange(sx < 0 ? new[] { c, c + 1 + i, c + 2 + i } : new[] { c, c + 2 + i, c + 1 + i });
            }
            var mesh = new Mesh();
            mesh.SetVertices(v);
            mesh.SetTriangles(t, 0);
            var uv = new Vector2[v.Count];
            for (int i = 0; i < v.Count; i++) uv[i] = new Vector2(v[i].x + v[i].z, v[i].y);
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return HegraKit.SaveMesh(mesh, name);
        }
    }
}
