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
