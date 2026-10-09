using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SecretOfHegra.EditorTools
{
    /// <summary>
    /// Rough rendering cost from fixed viewpoints: renderers (≈ draw calls before batching) and triangles inside the
    /// view frustum (72° FOV, 150 m far plane, like the headset). Used for before/after comparisons.
    /// </summary>
    public static class HegraStats
    {
        static readonly (string name, Vector3 eye, Vector3 target)[] s_Views =
        {
            ("camp_spawn", new Vector3(0f, 1.6f, -1.5f), new Vector3(0f, 1.4f, 10f)),
            ("market_entry", new Vector3(0f, 1.6f, 21f), new Vector3(0f, 1.4f, 32f)),
            ("valley", new Vector3(2f, 1.6f, 30f), new Vector3(50f, 6f, 58f)),
            ("farid_rock", new Vector3(92f, 1.6f, 27f), new Vector3(101f, 4f, 16f)),
        };

        public static string Measure()
        {
            var sb = new StringBuilder();
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
            long sceneTris = 0;
            int sceneCount = 0;
            foreach (var r in renderers)
            {
                if (!r.enabled || r is ParticleSystemRenderer) continue;
                sceneCount += Mathf.Max(1, r.sharedMaterials.Length);
                sceneTris += Tris(r);
            }
            sb.AppendLine($"scene: {sceneCount} draws, {sceneTris / 1000}k tris");
            var go = new GameObject("TmpStatsCam") { hideFlags = HideFlags.HideAndDontSave };
            var cam = go.AddComponent<Camera>();
            cam.enabled = false;
            cam.fieldOfView = 72f;
            cam.farClipPlane = 150f;
            cam.aspect = 1f;
            try
            {
                foreach (var v in s_Views)
                {
                    go.transform.SetPositionAndRotation(v.eye, Quaternion.LookRotation(v.target - v.eye));
                    var planes = GeometryUtility.CalculateFrustumPlanes(cam);
                    int draws = 0; long tris = 0;
                    foreach (var r in renderers)
                    {
                        if (!r || !r.enabled || !r.gameObject.activeInHierarchy || r is ParticleSystemRenderer) continue;
                        if (!GeometryUtility.TestPlanesAABB(planes, r.bounds)) continue;
                        draws += Mathf.Max(1, r.sharedMaterials.Length);
                        tris += Tris(r);
                    }
                    sb.AppendLine($"{v.name}: {draws} draws, {tris / 1000}k tris");
                }
            }
            finally { Object.DestroyImmediate(go); }
            return sb.ToString();
        }

        static long Tris(Renderer r)
        {
            Mesh m = null;
            if (r is MeshRenderer) { var mf = r.GetComponent<MeshFilter>(); if (mf) m = mf.sharedMesh; }
            else if (r is SkinnedMeshRenderer smr) m = smr.sharedMesh;
            if (!m) return 2;
            long t = 0;
            for (int i = 0; i < m.subMeshCount; i++) t += m.GetIndexCount(i) / 3;
            return t;
        }
    }
}
