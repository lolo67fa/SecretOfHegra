using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SecretOfHegra.EditorTools
{
    public static partial class HegraBuilder
    {
        /// <summary>A ring of small pips (the gaze-hold indicator), hidden until used.</summary>
        static GazeRing BuildGazeRing(Transform parent)
        {
            var go = HegraKit.Group("GazeRing", parent, Vector3.zero).transform;
            var ring = go.gameObject.AddComponent<GazeRing>();
            ring.onMaterial = M.glowWarm;
            ring.offMaterial = HegraKit.Unlit("M_GazePipOff", new Color(0.35f, 0.3f, 0.42f));
            const int count = 16;
            for (int i = 0; i < count; i++)
            {
                float a = i / (float)count * Mathf.PI * 2f;
                var p = new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0) * 0.32f;
                var pip = HegraKit.Prim(PrimitiveType.Sphere, go, "Pip", p, Vector3.one * 0.06f, ring.offMaterial);
                ring.pips.Add(pip.GetComponent<Renderer>());
            }
            go.gameObject.SetActive(false);
            return ring;
        }

        // ───────────── Stop 5: Qasr al-Bint ─────────────

        static BintStop BuildBint(Transform world, HegraAudioAssets.Set audio)
        {
            float yaw = YawTo(IthlibPos, BintPos);
            var root = HegraKit.Group("Stop5_QasrAlBint", world, BintPos, yaw).transform;
            var stop = root.gameObject.AddComponent<BintStop>();
            stop.stopId = StopId.Bint;
            stop.spawnPoint = Point(root, "SpawnPoint", new Vector3(0, 0, -7.5f), new Vector3(0, 0, 5f));
            stop.guidePoint = Point(root, "GuidePoint", new Vector3(-2.4f, 0, -2.2f), new Vector3(0, 0, -7f));
            stop.facadeCenter = Point(root, "FacadeCenter", new Vector3(0, 2f, 8.5f), new Vector3(0, 2f, 0));

            // great rock-cut facade: cliff, pilasters, cornices, crow-step crown and a dark doorway
            var decor = HegraKit.Group("Decor", root, Vector3.zero).transform;
            var cliff = RockMeshes("BintCliff", 47, 20, 12, 0.22f, 0.2f);
            // kept behind the facade plane and below the stars so the eagle and sky stay in view from the watching stone
            RockObject("Cliff", decor, cliff, new Vector3(0, 0, 14.8f), new Vector3(0, 10, 0), new Vector3(16f, 8f, 4f), true);
            RockObject("CliffW", decor, cliff, new Vector3(-13f, 0, 12f), new Vector3(0, 70, 0), new Vector3(6f, 6f, 4f), true);
            var face = HegraKit.Group("Facade", decor, new Vector3(0, 0, 9f)).transform;
            HegraKit.Prim(PrimitiveType.Cube, face, "Face", new Vector3(0, 3.6f, 0.4f), new Vector3(7.6f, 7.2f, 0.8f), M.sandstoneLit, null, true);
            foreach (var x in new[] { -3.3f, 3.3f, -1.6f, 1.6f })
                HegraKit.Prim(PrimitiveType.Cube, face, "Pilaster", new Vector3(x, 2.6f, -0.05f), new Vector3(0.42f, 5.2f, 0.2f), M.sandstoneShadow);
            HegraKit.Prim(PrimitiveType.Cube, face, "CorniceLow", new Vector3(0, 5.3f, -0.1f), new Vector3(7.9f, 0.22f, 0.4f), M.sandstoneShadow);
            HegraKit.Prim(PrimitiveType.Cube, face, "CorniceHigh", new Vector3(0, 6.2f, -0.1f), new Vector3(7.7f, 0.18f, 0.35f), M.sandstoneShadow);
            CrowSteps(face, new Vector3(0, 7.2f, 0.3f), 7.4f, 0.45f, M.sandstoneLit);
            HegraKit.Prim(PrimitiveType.Cube, face, "Doorway", new Vector3(0, 1.4f, -0.02f), new Vector3(1.5f, 2.8f, 0.02f), HegraKit.Unlit("M_Dark", new Color(0.03f, 0.02f, 0.03f)));
            HegraKit.Prim(PrimitiveType.Cube, face, "DoorFrame", new Vector3(0, 2.95f, -0.08f), new Vector3(2.0f, 0.18f, 0.12f), M.sandstoneShadow);
            HegraKit.Prim(PrimitiveType.Cube, face, "Pediment", new Vector3(0, 3.35f, -0.08f), new Vector3(2.3f, 0.14f, 0.12f), M.sandstoneShadow);
            HegraKit.Prim(PrimitiveType.Cube, face, "Steps", new Vector3(0, 0.12f, -0.6f), new Vector3(2.4f, 0.24f, 0.8f), M.sandstoneShadow, null, true);
            HegraKit.MarkStatic(decor.gameObject);

            // the eagle above the doorway (carved; glows and spreads its wings when woken)
            var eagle = EagleShape(face, "Eagle", new Vector3(0, 4.25f, -0.2f), 1.5f, M.sandstoneLit);
            eagle.localRotation = Quaternion.Euler(0, 180f, 0); // face the visitor
            stop.eagle = eagle;
            stop.eagleWingL = eagle.Find("WingL");
            stop.eagleWingR = eagle.Find("WingR");
            stop.eagleBeak = eagle.Find("Beak");
            var eglow = HegraKit.Prim(PrimitiveType.Quad, eagle, "WakeGlow", new Vector3(0, 0.1f, 0.1f), Vector3.one * 2.2f, M.glowWarm, new Vector3(0, 180f, 0));
            stop.eagleGlow = eglow.GetComponent<Renderer>();
            var eCol = eagle.gameObject.AddComponent<BoxCollider>();
            eCol.size = new Vector3(1.4f, 1.2f, 0.4f);
            eCol.center = new Vector3(0, 0.2f, 0);
            stop.eagleInteractable = eagle.gameObject.AddComponent<XRSimpleInteractable>();

            // the watching stone the player stands on
            stop.watchingStone = Point(root, "WatchingStone", new Vector3(0, 0, -1.2f), new Vector3(0, 0, 8f));
            var stone = HegraKit.MeshObject("Stone", stop.watchingStone, HegraKit.Rock("WatchStone", 53, 10, 8, 0.12f, 0.8f), M.sandstoneLit, new Vector3(0, -0.06f, 0), null, new Vector3(1.6f, 0.22f, 1.4f));
            HegraKit.MeshObject("StoneGlow", stop.watchingStone, HegraKit.GroundQuad(), M.glowPool, new Vector3(0, 0.07f, 0), null, new Vector3(2.2f, 1f, 2.2f));
            var mark = EagleShape(stop.watchingStone, "StoneEagleMark", new Vector3(0, 0.1f, 0), 0.35f, M.sandstoneShadow);
            mark.localRotation = Quaternion.Euler(90f, 0, 0);

            // five stars above and behind the facade; the eagle gazes at one of them
            var sky = HegraKit.Group("BintStars", root, Vector3.zero).transform;
            var starPos = new[]
            {
                new Vector3(-16f, 30f, 32f), new Vector3(-6f, 35f, 36f), new Vector3(5f, 29f, 34f),
                new Vector3(14f, 34f, 30f), new Vector3(1f, 41f, 30f),
            };
            const int correct = 3;
            stop.correctStar = correct;
            for (int i = 0; i < starPos.Length; i++)
            {
                var s = HegraKit.Prim(PrimitiveType.Sphere, sky, "Star" + i, starPos[i], Vector3.one * 0.9f, M.moon);
                var halo = HegraKit.Prim(PrimitiveType.Quad, s.transform, "Twinkle", Vector3.zero, Vector3.one * 3.2f, M.glowStar);
                halo.AddComponent<FlameFlicker>().billboardYaw = true;
                var col = s.AddComponent<SphereCollider>();
                col.radius = 1.6f;
                stop.stars.Add(s.transform);
                stop.starInteractables.Add(s.AddComponent<XRSimpleInteractable>());
                if (i == correct)
                {
                    var big = HegraKit.Prim(PrimitiveType.Quad, s.transform, "AlignedHalo", Vector3.zero, Vector3.one * 7f, M.glowWarm);
                    big.AddComponent<FlameFlicker>().billboardYaw = true;
                    stop.correctStarHalo = big.GetComponent<Renderer>();
                    stop.correctStarHalo.enabled = false;
                }
            }

            // thread of light from the eagle's beak to the chosen star
            var beakWorld = stop.eagleBeak.position;
            var starWorld = stop.stars[correct].position;
            var mid = (beakWorld + starWorld) * 0.5f;
            var dir = starWorld - beakWorld;
            var thread = HegraKit.Prim(PrimitiveType.Cube, root, "LightThread", root.InverseTransformPoint(mid), new Vector3(0.06f, 0.06f, dir.magnitude), M.glowWarm);
            thread.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            thread.SetActive(false);
            stop.lightThread = thread.transform;

            stop.ring = BuildGazeRing(root);

            // onward to Qasr al-Farid
            var toFarid = FaridPos - BintPos; toFarid.y = 0; toFarid.Normalize();
            stop.toFarid = Point(root, "ToFarid", root.InverseTransformPoint(BintPos + toFarid * 9f) + Vector3.up * 1.5f, Vector3.zero);

            Lantern(root, new Vector3(-1.8f, 0, -2.6f), 1.6f);
            Lantern(root, new Vector3(1.8f, 0, -2.6f), 1.6f);
            Lantern(root, new Vector3(-3.6f, 0, 7.4f), 2.3f);
            Lantern(root, new Vector3(3.6f, 0, 7.4f), 2.3f);

            var zone = root.gameObject.AddComponent<AmbienceZone>();
            zone.stop = StopId.Bint;
            zone.radius = 14f;
            zone.music = null; // journey music plays throughout (AudioDirector)
            zone.ambience = audio.Get("amb_wind");
            zone.ambienceVolume = 0.2f;

            EduCardObject(root, new Vector3(4.2f, 0, -4.6f), "edu.bint");
            return stop;
        }
    }
}
