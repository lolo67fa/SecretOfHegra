using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SecretOfHegra.EditorTools
{
    public static partial class HegraBuilder
    {
        // ───────────── particle effects (all additive glow sprites, light enough for Quest) ─────────────

        static ParticleSystem NewParticles(string name, Transform parent, Vector3 localPos, Material mat, int maxParticles)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.minParticleSize = 0f;
            r.maxParticleSize = 0.5f;
            return ps;
        }

        static AnimationCurve RiseAndFade() => new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0f));

        static Gradient FadeInOut(Color c)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        /// <summary>A puff of golden dust that rises out of the chest the moment the lid opens.</summary>
        static ParticleSystem TreasureBurst(Transform chest, Material mat, float rimHeight, float halfWidth, float halfDepth)
        {
            var ps = NewParticles("TreasureBurst", chest, new Vector3(0, rimHeight, 0), mat, 90);
            var main = ps.main;
            main.loop = false;
            main.duration = 1.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.gravityModifier = 0.08f;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 55), new ParticleSystem.Burst(0.35f, 30) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.rotation = new Vector3(-90f, 0, 0);   // emit upward (the box is rotated, so its depth is local y)
            shape.scale = new Vector3(halfWidth * 1.8f, halfDepth * 1.8f, 0.02f);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = FadeInOut(new Color(1f, 0.85f, 0.5f));
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.25f;
            noise.frequency = 0.6f;
            return ps;
        }

        /// <summary>Little glints that keep appearing over the gold while the chest stands open.</summary>
        static ParticleSystem TreasureSparkle(Transform chest, Material mat, float goldHeight, float halfWidth, float halfDepth)
        {
            var ps = NewParticles("TreasureSparkle", chest, new Vector3(0, goldHeight, 0), mat, 30);
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.09f);
            var emission = ps.emission;
            emission.rateOverTime = 18f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(halfWidth * 1.7f, 0.12f, halfDepth * 1.7f);
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, RiseAndFade());
            return ps;
        }

        /// <summary>Faint warm dust motes drifting in the night air around the player (they stay in the world).</summary>
        static void AirMotes(Transform rig)
        {
            var mat = HegraKit.GlowParticle("M_AirMote", new Color(1f, 0.86f, 0.62f), 0.7f, 2.2f);
            var ps = NewParticles("AirMotes", rig, new Vector3(0, 1.4f, 0), mat, 70);
            var main = ps.main;
            main.playOnAwake = true;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.05f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.028f);
            var emission = ps.emission;
            emission.rateOverTime = 7f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 5f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = FadeInOut(Color.white);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.08f;
            noise.frequency = 0.25f;
        }

        // ───────────── speaking characters ─────────────

        /// <summary>
        /// Adds the speech-bubble anchor (just above the head) and the soft glow shown above the head while the
        /// character speaks.
        /// </summary>
        static void SpeakerExtras(NpcFigure npc)
        {
            if (!npc || !npc.head) return;
            var root = npc.transform;
            var headLocal = root.InverseTransformPoint(npc.head.position);
            var anchor = new GameObject("DialogueAnchor").transform;
            anchor.SetParent(root, false);
            anchor.localPosition = headLocal + Vector3.up * 0.16f;
            npc.dialogueAnchor = anchor;

            var glow = HegraKit.Prim(PrimitiveType.Quad, root, "SpeakGlow", headLocal + Vector3.up * 0.36f, Vector3.zero, M.glowWarm);
            var r = glow.GetComponent<Renderer>();
            r.enabled = false;
            npc.speakGlow = r;
        }

        // ───────────── draw-call savers ─────────────

        /// <summary>
        /// Merges the plain mesh pieces under root into one mesh per material (saved as assets). Pieces with a collider
        /// or any behaviour (handles, interactables) are left alone. recursive=false only looks at direct children.
        /// </summary>
        static void CombineByMaterial(Transform root, string name, bool recursive)
        {
            var groups = new Dictionary<Material, List<CombineInstance>>();
            var merged = new List<GameObject>();
            var filters = recursive ? root.GetComponentsInChildren<MeshFilter>(true) : DirectFilters(root);
            foreach (var mf in filters)
            {
                var mr = mf.GetComponent<MeshRenderer>();
                if (!mr || !mf.sharedMesh || !mr.sharedMaterial) continue;
                if (mf.GetComponent<Collider>() || mf.GetComponent<MonoBehaviour>() || mf.transform.childCount > 0) continue;
                // animated characters and LOD groups keep their own pieces
                if (mf.GetComponentInParent<NpcFigure>() || mf.GetComponentInParent<LODGroup>() || mf.GetComponentInParent<NpcWalker>()) continue;
                if (!groups.TryGetValue(mr.sharedMaterial, out var list)) groups[mr.sharedMaterial] = list = new List<CombineInstance>();
                list.Add(new CombineInstance { mesh = mf.sharedMesh, transform = root.worldToLocalMatrix * mf.transform.localToWorldMatrix });
                merged.Add(mf.gameObject);
            }
            foreach (var kv in groups)
            {
                if (kv.Value.Count < 2) { merged.RemoveAll(g => g.GetComponent<MeshRenderer>().sharedMaterial == kv.Key); continue; }
                var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(kv.Value.ToArray(), true, true);
                mesh.RecalculateBounds();
                var saved = HegraKit.SaveMesh(mesh, $"{name}_{kv.Key.name}");
                HegraKit.MeshObject($"{name}_{kv.Key.name}", root, saved, kv.Key, Vector3.zero);
            }
            foreach (var g in merged) if (g) Object.DestroyImmediate(g);
            // groups emptied by the merge (e.g. goblets) go too
            if (recursive)
            {
                var all = root.GetComponentsInChildren<Transform>(true);
                for (int i = all.Length - 1; i >= 0; i--)   // children before parents
                {
                    var t = all[i];
                    if (t && t != root && t.childCount == 0 && t.GetComponents<Component>().Length == 1) Object.DestroyImmediate(t.gameObject);
                }
            }
        }

        static MeshFilter[] DirectFilters(Transform root)
        {
            var list = new List<MeshFilter>();
            foreach (Transform c in root) { var mf = c.GetComponent<MeshFilter>(); if (mf) list.Add(mf); }
            return list.ToArray();
        }
    }
}
