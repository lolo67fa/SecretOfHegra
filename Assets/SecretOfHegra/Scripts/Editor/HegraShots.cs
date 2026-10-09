using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace SecretOfHegra.EditorTools
{
    /// <summary>
    /// Renders the open scene from fixed viewpoints into &lt;project&gt;/Screenshots so before/after comparisons
    /// use exactly the same angles. Editor only; uses a temporary non-XR camera.
    /// </summary>
    public static class HegraShots
    {
        static readonly (string name, Vector3 eye, Vector3 target)[] s_Views =
        {
            ("valley_from_market", new Vector3(2f, 1.6f, 30f), new Vector3(50f, 6f, 58f)),
            ("ithlib_massif", new Vector3(44f, 1.6f, 50f), new Vector3(50f, 4f, 66f)),
            ("bint_facade", new Vector3(70f, 1.6f, 48f), new Vector3(85f, 5f, 40f)),
            ("farid_rock", new Vector3(92f, 1.6f, 27f), new Vector3(101f, 4f, 16f)),
            ("horizon_from_camp", new Vector3(0f, 1.6f, -4f), new Vector3(-60f, 12f, 120f)),
        };

        /// <summary>
        /// Batch-mode entry: renders the treasure chest (closed, then open with its glow) into Screenshots.
        /// -executeMethod SecretOfHegra.EditorTools.HegraShots.CaptureChestBatch
        /// </summary>
        public static void CaptureChestBatch()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(HegraBuilder.ScenePath);
            var stop = Object.FindAnyObjectByType<FaridStop>();
            if (!stop || !stop.chestLid) { Debug.LogError("[Hegra] No chest found"); UnityEditor.EditorApplication.Exit(1); return; }
            if (stop.chamberLight) { stop.chamberLight.enabled = true; stop.chamberLight.intensity = stop.chamberLightIntensity; }
            var chest = stop.chestLid.parent;
            var look = chest.position + Vector3.up * 0.35f;
            var views = new[]
            {
                ("front", chest.position - chest.forward * 1.5f + Vector3.up * 1.45f),
                ("side", chest.position - chest.forward * 1.1f + chest.right * 0.9f + Vector3.up * 1.2f),
            };
            var closed = stop.chestLid.localRotation;
            foreach (var open in new[] { false, true })
            {
                stop.chestLid.localRotation = open ? closed * Quaternion.Euler(stop.lidOpenEuler) : closed;
                if (stop.treasureLight) { stop.treasureLight.enabled = open; stop.treasureLight.intensity = stop.treasureLightIntensity; }
                if (open && stop.treasureBurst) stop.treasureBurst.Simulate(0.7f, true, true);
                if (open && stop.treasureSparkle) stop.treasureSparkle.Simulate(2f, true, true);
                foreach (var (name, eye) in views)
                    Debug.Log("[Hegra] " + Render($"chest_{name}_{(open ? "open" : "closed")}", eye, look, 1280, 720));
            }
            stop.chestLid.localRotation = closed;
        }

        static string Render(string file, Vector3 eye, Vector3 target, int width, int height)
        {
            var dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Screenshots");
            Directory.CreateDirectory(dir);
            var go = new GameObject("TmpShotCam") { hideFlags = HideFlags.HideAndDontSave };
            var cam = go.AddComponent<Camera>();
            cam.stereoTargetEye = StereoTargetEyeMask.None;
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.05f;
            cam.enabled = false;
            go.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(target - eye));
            var prevPipeline = QualitySettings.renderPipeline;
            var quest = UnityEditor.AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(HegraKit.Root + "/Generated/HegraQuest_RPAsset.asset");
            if (quest) QualitySettings.renderPipeline = quest;
            var rt = new RenderTexture(width, height, 24);
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            var path = Path.Combine(dir, file + ".png");
            try
            {
                RenderPipeline.SubmitRenderRequest(cam, new RenderPipeline.StandardRequest { destination = rt });
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                File.WriteAllBytes(path, tex.EncodeToPNG());
            }
            finally
            {
                QualitySettings.renderPipeline = prevPipeline;
                rt.Release();
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(go);
            }
            return path;
        }

        public static string Capture(string tag, int width = 1280, int height = 720)
        {
            var dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Screenshots");
            Directory.CreateDirectory(dir);
            var go = new GameObject("TmpShotCam") { hideFlags = HideFlags.HideAndDontSave };
            var cam = go.AddComponent<Camera>();
            cam.stereoTargetEye = StereoTargetEyeMask.None;
            cam.fieldOfView = 70f;
            cam.farClipPlane = 1000f;
            cam.enabled = false;
            // render with the Quest pipeline asset so shots match the headset (no SSAO/HDR, MSAA 4x)
            var prevPipeline = QualitySettings.renderPipeline;
            var quest = UnityEditor.AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(HegraKit.Root + "/Generated/HegraQuest_RPAsset.asset");
            if (quest) QualitySettings.renderPipeline = quest;
            var rt = new RenderTexture(width, height, 24);
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            int n = 0;
            try
            {
                foreach (var v in s_Views)
                {
                    go.transform.position = v.eye;
                    go.transform.rotation = Quaternion.LookRotation(v.target - v.eye);
                    var req = new RenderPipeline.StandardRequest { destination = rt };
                    RenderPipeline.SubmitRenderRequest(cam, req);
                    var prev = RenderTexture.active;
                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    tex.Apply();
                    RenderTexture.active = prev;
                    File.WriteAllBytes(Path.Combine(dir, $"{v.name}_{tag}.png"), tex.EncodeToPNG());
                    n++;
                }
            }
            finally
            {
                QualitySettings.renderPipeline = prevPipeline;
                rt.Release();
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(go);
            }
            return $"{n} shots → {dir}";
        }
    }
}
