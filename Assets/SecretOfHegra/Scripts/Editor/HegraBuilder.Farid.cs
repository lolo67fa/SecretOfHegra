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

            // the caravan chest with a hinged lid
            var chest = HegraKit.Group("CaravanChest", ch, new Vector3(0, 0, 0.6f)).transform;
            HegraKit.Prim(PrimitiveType.Cube, chest, "Box", new Vector3(0, 0.27f, 0), new Vector3(1.0f, 0.54f, 0.62f), M.wood, null, true);
            foreach (var x in new[] { -0.38f, 0.38f })
                HegraKit.Prim(PrimitiveType.Cube, chest, "Band", new Vector3(x, 0.27f, 0), new Vector3(0.06f, 0.56f, 0.64f), M.bronze);
            var treasure = HegraKit.Group("Treasure", chest, new Vector3(0, 0.5f, 0)).transform;
            for (int i = 0; i < 9; i++)
                HegraKit.Prim(PrimitiveType.Cylinder, treasure, "Coin", new Vector3(-0.3f + (i % 3) * 0.1f, 0.02f + (i / 3) * 0.012f, -0.1f + (i % 2) * 0.08f),
                    new Vector3(0.06f, 0.006f, 0.06f), M.coinGold, new Vector3((i * 23) % 20, 0, 0));
            for (int i = 0; i < 6; i++)
                HegraKit.Prim(PrimitiveType.Sphere, treasure, "Frankincense", new Vector3(0.12f + (i % 3) * 0.07f, 0.03f, -0.12f + (i / 3) * 0.08f), Vector3.one * 0.05f, M.resin);
            HegraKit.Prim(PrimitiveType.Cylinder, treasure, "CaravanSeal", new Vector3(0.05f, 0.04f, 0.15f), new Vector3(0.1f, 0.03f, 0.1f), M.bronze);
            stop.treasure = treasure.gameObject;
            var lidPivot = HegraKit.Group("LidPivot", chest, new Vector3(0, 0.54f, 0.31f)).transform;
            HegraKit.Prim(PrimitiveType.Cube, lidPivot, "Lid", new Vector3(0, 0.05f, -0.31f), new Vector3(1.02f, 0.1f, 0.64f), M.wood);
            HegraKit.Prim(PrimitiveType.Cube, lidPivot, "LidBand", new Vector3(0, 0.05f, -0.31f), new Vector3(1.04f, 0.11f, 0.06f), M.bronze);
            var handle = HegraKit.Prim(PrimitiveType.Cube, lidPivot, "LidHandle", new Vector3(0, 0.08f, -0.64f), new Vector3(0.22f, 0.06f, 0.06f), M.bronze, null, true);
            stop.chestLid = lidPivot;
            stop.lidOpenEuler = new Vector3(105f, 0, 0); // front edge swings up and back
            stop.lidHandle = handle.AddComponent<XRSimpleInteractable>();

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
    }
}
