using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace SecretOfHegra.EditorTools
{
    /// <summary>
    /// Tools ▸ Secret of Hegra ▸ Validate. Checks the scene for missing scripts and unassigned references,
    /// the data files for missing localization/dialogue entries, and the project for XR/Android setup.
    /// </summary>
    public static class HegraValidator
    {
        // Serialized references that are intentionally optional.
        static readonly HashSet<string> s_Optional = new HashSet<string>
        {
            "homeSocket", "m_Voiceover", "music", "ambience", "markerVisual", "camel", "guidePoint", "femaleOnly", "maleOnly",
            "head", "leftArm", "rightArm", "body", "figure", "skyMaterial", "floorReference", "m_Vignette", "m_ContinuousTurn",
            "m_Move", "arabic", "english", "m_Subtitles", "m_Listener", "watchedCoin", "waterN", "waterE", "waterS", "waterW",
            "ignoreUnder"
        };

        [MenuItem("Tools/Secret of Hegra/Validate", priority = 1)]
        public static void ValidateMenu()
        {
            var issues = Validate(true);
            EditorUtility.DisplayDialog("Secret of Hegra — Validate",
                issues.Count == 0 ? "No issues found." : $"{issues.Count} issue(s):\n\n" + string.Join("\n", issues.Take(25)), "OK");
        }

        public static List<string> Validate(bool openScene)
        {
            var issues = new List<string>();
            if (!File.Exists(HegraBuilder.ScenePath))
            {
                issues.Add("Scene missing: run Tools ▸ Secret of Hegra ▸ Build or Repair Game.");
                Report(issues);
                return issues;
            }
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != HegraBuilder.ScenePath)
            {
                if (!openScene) { issues.Add("Scene is not open; validation of scene objects skipped."); }
                else
                {
                    if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return issues;
                    scene = EditorSceneManager.OpenScene(HegraBuilder.ScenePath);
                }
            }

            if (scene.path == HegraBuilder.ScenePath)
            {
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    {
                        int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                        if (missing > 0) issues.Add($"Missing script(s) on {Path(t)}");
                        foreach (var mb in t.GetComponents<MonoBehaviour>())
                        {
                            if (!mb || mb.GetType().Namespace != "SecretOfHegra") continue;
                            var so = new SerializedObject(mb);
                            var p = so.GetIterator();
                            while (p.NextVisible(true))
                            {
                                if (p.propertyType != SerializedPropertyType.ObjectReference) continue;
                                if (p.objectReferenceValue != null) continue;
                                var name = p.name == "data" ? ParentName(p.propertyPath) : p.name;
                                if (s_Optional.Contains(name)) continue;
                                // only check fields declared by this game's types (XRI base fields have their own defaults)
                                if (!DeclaredByGame(mb.GetType(), p.propertyPath.Split('.')[0])) continue;
                                issues.Add($"Unassigned or broken {mb.GetType().Name}.{p.propertyPath} on {Path(t)}");
                            }
                        }
                    }

                var gm = Object.FindAnyObjectByType<GameManager>();
                if (!gm) issues.Add("No GameManager in scene.");
                else
                {
                    if (!gm.DataConfigured) issues.Add("GameManager data (strings, dialogue, Nabataean map, font) not assigned.");
                    if (!gm.SystemsConfigured) issues.Add("GameManager systems not fully assigned.");
                    var handled = new HashSet<StopId>(gm.Stops.Where(s => s).Select(s => s.stopId));
                    foreach (StopId id in System.Enum.GetValues(typeof(StopId)))
                        if (!handled.Contains(id)) issues.Add($"No stop controller for {id} (its steps cannot progress).");
                }
                if (!Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>()) issues.Add("No EventSystem (XR UI will not respond).");
                ValidateFirstSign(issues);
                ValidateMusic(issues);
            }

            ValidateData(issues);
            ValidateProject(issues);
            Report(issues);
            return issues;
        }

        /// <summary>Sign 1 comes only from the Elephant Rock puzzle (the old theater wall no longer grants it).</summary>
        static void ValidateFirstSign(List<string> issues)
        {
            var theater = Object.FindAnyObjectByType<TheaterStop>(FindObjectsInactive.Include);
            if (!theater) return;
            var rock = theater.rock;
            if (!rock) { issues.Add("TheaterStop has no Elephant Rock puzzle (Sign 1)."); return; }
            if (theater.inscription != rock.reveal) issues.Add("TheaterStop.inscription is not the Elephant Rock's lamp reveal.");
            var reveals = Object.FindObjectsByType<LampRevealTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var r in reveals)
                if (r != rock.reveal) issues.Add($"Extra lamp-reveal target {Path(r.transform)} (only the Elephant Rock should reveal Sign 1).");
            if (rock.glyphs.Count != rock.glyphButtons.Count || rock.glyphs.Count != rock.glyphGlows.Count)
                issues.Add("Elephant Rock glyph lists differ in length.");
            if (rock.order == null || rock.order.Length != 3 || rock.order.Any(i => i < 0 || i >= rock.glyphs.Count))
                issues.Add("Elephant Rock sequence must name three valid glyphs.");
            foreach (var b in rock.glyphButtons)
                if (b && b.colliders.Count == 0 && !b.GetComponent<Collider>()) issues.Add($"Glyph {b.name} has no collider for the ray.");
        }

        /// <summary>One journey track from the menu to the ending: no zone music and no other music-group sources.</summary>
        static void ValidateMusic(List<string> issues)
        {
            var director = Object.FindAnyObjectByType<AudioDirector>(FindObjectsInactive.Include);
            if (!director) { issues.Add("No AudioDirector in scene."); return; }
            if (!director.JourneyMusic) issues.Add("AudioDirector has no journey music assigned.");
            foreach (var z in Object.FindObjectsByType<AmbienceZone>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (z.music) issues.Add($"Ambience zone {Path(z.transform)} still sets its own music ({z.music.name}).");
            foreach (var src in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (src.outputAudioMixerGroup && src.outputAudioMixerGroup == director.MusicGroup)
                    issues.Add($"Scene audio source {Path(src.transform)} plays into the Music group (would compete with the journey track).");
        }

        static bool DeclaredByGame(System.Type type, string field)
        {
            for (var t = type; t != null && t != typeof(MonoBehaviour); t = t.BaseType)
            {
                var f = t.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly);
                if (f != null) return t.Namespace == "SecretOfHegra";
            }
            return false;
        }

        static string ParentName(string path)
        {
            var m = Regex.Match(path, @"(\w+)\.Array\.data\[\d+\]$");
            return m.Success ? m.Groups[1].Value : path;
        }

        static string Path(Transform t) => t.parent ? Path(t.parent) + "/" + t.name : t.name;

        static void ValidateData(List<string> issues)
        {
            var ui = AssetDatabase.LoadAssetAtPath<TextAsset>(HegraKit.Root + "/Data/ui_strings.json");
            var dlg = AssetDatabase.LoadAssetAtPath<TextAsset>(HegraKit.Root + "/Data/dialogue.json");
            if (!ui || !dlg) { issues.Add("Data files missing in Assets/SecretOfHegra/Data."); return; }
            Loc.Clear();
            Loc.LoadTable(ui.text);
            var db = DialogueDatabase.FromJson(dlg.text);

            foreach (var info in QuestFlow.Steps)
                if (info.step != QuestStep.Complete && !Loc.Has("obj." + info.step))
                    issues.Add($"Missing objective text obj.{info.step}");

            foreach (var seq in db.Sequences)
                foreach (var line in seq.lines)
                {
                    if (!Loc.Has(line.key)) { issues.Add($"Dialogue line '{line.key}' has no text"); continue; }
                    if (string.IsNullOrWhiteSpace(Loc.Get(line.key, Language.Arabic)) || string.IsNullOrWhiteSpace(Loc.Get(line.key, Language.English)))
                        issues.Add($"Dialogue line '{line.key}' is missing a language");
                    if (db.GetSpeaker(line.speaker) == null) issues.Add($"Dialogue line '{line.key}' has unknown speaker '{line.speaker}'");
                }

            // every sequence id used in code must exist
            var code = string.Join("\n", Directory.GetFiles(HegraKit.Root + "/Scripts/Runtime", "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
            foreach (Match m in Regex.Matches(code, @"Dialogue\.Play\(\s*""([a-z0-9_]+)"""))
                if (!db.Has(m.Groups[1].Value)) issues.Add($"Code plays missing dialogue sequence '{m.Groups[1].Value}'");
            foreach (Match m in Regex.Matches(code, @"""((?:ui|toast|menu|set|inv|assist|end|sign|edu|stop)\.[a-z0-9_.]+)"""))
                if (!Loc.Has(m.Groups[1].Value) && !m.Groups[1].Value.EndsWith(".")) issues.Add($"Code uses missing string '{m.Groups[1].Value}'");
            foreach (Match m in Regex.Matches(code, @"Play\(\s*[^""]*\?\s*""([a-z0-9_]+)""\s*:\s*""([a-z0-9_]+)"""))
            {
                if (!db.Has(m.Groups[1].Value)) issues.Add($"Code plays missing dialogue sequence '{m.Groups[1].Value}'");
                if (!db.Has(m.Groups[2].Value)) issues.Add($"Code plays missing dialogue sequence '{m.Groups[2].Value}'");
            }

            // educational cards in the scene
            foreach (var card in Object.FindObjectsByType<EduCard>(FindObjectsInactive.Include))
                if (!Loc.Has(card.titleKey) || !Loc.Has(card.bodyKey)) issues.Add($"Educational card '{card.titleKey}' text missing");
        }

        static void ValidateProject(List<string> issues)
        {
            if (!AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(HegraKit.Root + "/Fonts/TMP_Amiri.asset")) issues.Add("Arabic font asset TMP_Amiri missing.");
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(HegraAudioAssets.MixerPath);
            if (!mixer) issues.Add("Audio mixer missing.");
            else
                foreach (var p in new[] { "MusicVolume", "AmbienceVolume", "SfxVolume", "DialogueVolume" })
                    if (!mixer.GetFloat(p, out _)) issues.Add($"Audio mixer parameter '{p}' not exposed.");

            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.settingsKey, out XRGeneralSettingsPerBuildTarget perTarget);
            var android = perTarget ? perTarget.SettingsForBuildTarget(BuildTargetGroup.Android) : null;
            if (!android || android.Manager == null || !android.Manager.activeLoaders.Any(l => l && l.GetType().Name == "OpenXRLoader"))
                issues.Add("OpenXR loader not enabled for Android.");
            var ox = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (!ox) issues.Add("OpenXR Android settings missing.");
            else
            {
                if (!ox.GetFeatures().Any(f => f.GetType().Name == "MetaQuestFeature" && f.enabled)) issues.Add("Meta Quest support feature not enabled (Android).");
                if (!ox.GetFeatures().Any(f => (f.GetType().Name == "OculusTouchControllerProfile" || f.GetType().Name == "MetaQuestTouchPlusControllerProfile") && f.enabled))
                    issues.Add("No Quest controller interaction profile enabled (Android).");
            }
            if (!EditorBuildSettings.scenes.Any(s => s.enabled && s.path == HegraBuilder.ScenePath)) issues.Add("Game scene not enabled in Build Settings.");
        }

        static void Report(List<string> issues)
        {
            if (issues.Count == 0) Debug.Log("[Hegra Validate] No issues found.");
            foreach (var i in issues) Debug.LogWarning("[Hegra Validate] " + i);
        }
    }
}
