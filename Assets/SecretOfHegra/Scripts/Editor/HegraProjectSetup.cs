using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace SecretOfHegra.EditorTools
{
    /// <summary>
    /// Quest 3 configuration: OpenXR loader + Meta Quest feature + controller profiles for Android,
    /// single-pass instanced (multiview), Vulkan, IL2CPP/ARM64, ASTC, and a lean URP asset for the Android
    /// quality level. Standalone XR stays off so the editor uses the XR Interaction Simulator.
    /// </summary>
    public static class HegraProjectSetup
    {
        public const string QuestRpPath = HegraKit.Root + "/Generated/HegraQuest_RPAsset.asset";
        public const string QuestRendererPath = HegraKit.Root + "/Generated/HegraQuest_Renderer.asset";
        public const string ApkPath = "Builds/Quest/SecretOfHegra_dev.apk";
        /// <summary>Release build for headset play tests (no development overlay, no URP Rendering Debugger).</summary>
        public const string ReleaseApkPath = "Builds/Quest/SecretOfHegra.apk";

        [MenuItem("Tools/Secret of Hegra/Configure Quest Build (switches to Android)", priority = 20)]
        public static void ConfigureQuestMenu()
        {
            ConfigureAll(true);
            EditorUtility.DisplayDialog("Secret of Hegra", "Quest/Android configuration applied.\n" + ToolchainReport(), "OK");
        }

        public static void ConfigureAll(bool switchToAndroid)
        {
            ConfigurePlayer();
            ConfigureXR();
            ConfigureUrp();
            if (switchToAndroid && EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            AssetDatabase.SaveAssets();
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.productName = "Secret of Hegra";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.lulu.secretofhegra");
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)32;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            PlayerSettings.SetMobileMTRendering(NamedBuildTarget.Android, true);
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        }

        static void ConfigureXR()
        {
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.settingsKey, out XRGeneralSettingsPerBuildTarget perTarget);
            if (!perTarget)
            {
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                HegraKit.EnsureFolder("Assets/XR");
                AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, perTarget, true);
            }
            if (!perTarget.HasSettingsForBuildTarget(BuildTargetGroup.Android)) perTarget.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
            if (!perTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android)) perTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            var gs = perTarget.SettingsForBuildTarget(BuildTargetGroup.Android);
            gs.InitManagerOnStart = true;
            XRPackageMetadataStore.AssignLoader(gs.Manager, "UnityEngine.XR.OpenXR.OpenXRLoader", BuildTargetGroup.Android);
            EditorUtility.SetDirty(gs);
            EditorUtility.SetDirty(perTarget);

            FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
            var ox = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (ox)
            {
                ox.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
                foreach (var f in ox.GetFeatures())
                {
                    var n = f.GetType().Name;
                    if (n == "MetaQuestFeature" || n == "OculusTouchControllerProfile" || n == "MetaQuestTouchPlusControllerProfile")
                        f.enabled = true;
                }
                EditorUtility.SetDirty(ox);
            }
        }

        static void ConfigureUrp()
        {
            var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(QuestRpPath);
            if (!rp)
            {
                HegraKit.EnsureFolder(HegraKit.Root + "/Generated");
                if (!AssetDatabase.CopyAsset("Assets/Settings/Mobile_RPAsset.asset", QuestRpPath) ||
                    !AssetDatabase.CopyAsset("Assets/Settings/Mobile_Renderer.asset", QuestRendererPath))
                {
                    Debug.LogWarning("[Hegra] Mobile URP assets not found; Quest render settings not created.");
                    return;
                }
                rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(QuestRpPath);
            }
            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(QuestRendererPath);
            var so = new SerializedObject(rp);
            void Set(string prop, Action<SerializedProperty> f) { var p = so.FindProperty(prop); if (p != null) f(p); }
            Set("m_MSAA", p => p.intValue = 4);
            Set("m_SupportsHDR", p => p.boolValue = false);
            Set("m_RenderScale", p => p.floatValue = 1f);
            Set("m_RequireDepthTexture", p => p.boolValue = false);
            Set("m_RequireOpaqueTexture", p => p.boolValue = false);
            Set("m_MainLightShadowsSupported", p => p.boolValue = false);
            Set("m_AdditionalLightsRenderingMode", p => p.intValue = 1); // per pixel (only the lamp)
            Set("m_AdditionalLightsPerObjectLimit", p => p.intValue = 1);
            Set("m_AdditionalLightShadowsSupported", p => p.boolValue = false);
            Set("m_SoftShadowsSupported", p => p.boolValue = false);
            Set("m_UseSRPBatcher", p => p.boolValue = true);
            var list = so.FindProperty("m_RendererDataList");
            if (list != null && renderer) { list.arraySize = 1; list.GetArrayElementAtIndex(0).objectReferenceValue = renderer; }
            so.ApplyModifiedPropertiesWithoutUndo();

            if (renderer)
            {
                var rso = new SerializedObject(renderer);
                var feats = rso.FindProperty("m_RendererFeatures");
                if (feats != null && feats.arraySize > 0)
                {
                    // keep it lean: no full-screen effects on Quest
                    for (int i = feats.arraySize - 1; i >= 0; i--)
                    {
                        var f = feats.GetArrayElementAtIndex(i).objectReferenceValue;
                        if (f && f.GetType().Name.Contains("AmbientOcclusion")) ((ScriptableRendererFeature)f).SetActive(false);
                    }
                }
                rso.ApplyModifiedPropertiesWithoutUndo();
            }

            // assign to the Android default quality level (index 0, "Mobile")
            var qs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset").FirstOrDefault();
            if (qs)
            {
                var qso = new SerializedObject(qs);
                var levels = qso.FindProperty("m_QualitySettings");
                var androidDefault = qso.FindProperty("m_PerPlatformDefaultQuality");
                int idx = 0;
                for (int i = 0; androidDefault != null && i < androidDefault.arraySize; i++)
                {
                    var e = androidDefault.GetArrayElementAtIndex(i);
                    if (e.FindPropertyRelative("first")?.stringValue == "Android") idx = e.FindPropertyRelative("second").intValue;
                }
                if (levels != null && idx < levels.arraySize)
                {
                    levels.GetArrayElementAtIndex(idx).FindPropertyRelative("customRenderPipeline").objectReferenceValue = rp;
                    qso.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            EditorUtility.SetDirty(rp);
        }

        // ───────────── toolchain + APK ─────────────

        public static string AndroidPlayerDir => Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer");

        public static string ToolchainReport()
        {
            bool module = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android);
            bool sdk = Directory.Exists(Path.Combine(AndroidPlayerDir, "SDK"));
            bool ndk = Directory.Exists(Path.Combine(AndroidPlayerDir, "NDK"));
            bool jdk = Directory.Exists(Path.Combine(AndroidPlayerDir, "OpenJDK"));
            return $"Android module: {(module ? "OK" : "MISSING")}\nSDK: {(sdk ? "OK" : "MISSING")}\nNDK: {(ndk ? "OK" : "MISSING")}\nOpenJDK: {(jdk ? "OK" : "MISSING")}";
        }

        public static bool ToolchainReady(out string report)
        {
            report = ToolchainReport();
            return !report.Contains("MISSING");
        }

        [MenuItem("Tools/Secret of Hegra/Build Development APK (Quest)", priority = 21)]
        public static void BuildApkMenu()
        {
            var r = BuildDevelopmentApk();
            EditorUtility.DisplayDialog("Secret of Hegra", r, "OK");
        }

        [MenuItem("Tools/Secret of Hegra/Build Release APK (Quest)", priority = 22)]
        public static void BuildReleaseApkMenu()
        {
            var r = BuildApk(false);
            EditorUtility.DisplayDialog("Secret of Hegra", r, "OK");
        }

        public static string BuildDevelopmentApk() => BuildApk(true);

        /// <summary>
        /// Development builds enable URP's runtime Rendering Debugger (both thumbsticks open it and it captures
        /// controller input), so headset play tests use the release build.
        /// </summary>
        public static string BuildApk(bool development)
        {
            if (!ToolchainReady(out var tools))
                return "Android toolchain incomplete — install Android Build Support with SDK/NDK and OpenJDK via Unity Hub.\n" + tools;
            ConfigureAll(true);
            var apkPath = development ? ApkPath : ReleaseApkPath;
            Directory.CreateDirectory(Path.GetDirectoryName(apkPath));
            EditorUserBuildSettings.buildAppBundle = false;
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { HegraBuilder.ScenePath },
                locationPathName = apkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = development ? BuildOptions.Development : BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            var msg = $"Build {s.result}: {s.outputPath}\nSize: {s.totalSize / (1024f * 1024f):0.0} MB, errors: {s.totalErrors}, time: {s.totalTime}";
            Debug.Log("[Hegra] " + msg);
            return msg;
        }

        /// <summary>Batch-mode entry (release APK): -executeMethod SecretOfHegra.EditorTools.HegraProjectSetup.BuildApkBatch</summary>
        public static void BuildApkBatch()
        {
            var msg = BuildApk(false);
            if (!msg.StartsWith("Build Succeeded")) EditorApplication.Exit(1);
        }
    }
}
