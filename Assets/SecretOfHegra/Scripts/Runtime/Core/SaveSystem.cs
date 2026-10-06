using System;
using System.IO;
using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// Local JSON persistence for progress (checkpoints) and settings. Writes go to a temp file first
    /// and are then swapped in, so a crash or headset sleep mid-write cannot corrupt the save.
    /// </summary>
    public class SaveSystem
    {
        public const string ProgressFile = "hegra_progress.json";
        public const string SettingsFile = "hegra_settings.json";

        readonly string m_Directory;

        public SaveSystem(string directory = null)
        {
            m_Directory = string.IsNullOrEmpty(directory) ? Application.persistentDataPath : directory;
        }

        public string Directory => m_Directory;
        string ProgressPath => Path.Combine(m_Directory, ProgressFile);
        string SettingsPath => Path.Combine(m_Directory, SettingsFile);

        public bool HasProgress()
        {
            var s = LoadProgress();
            return s != null && s.step != QuestStep.None;
        }

        public void SaveProgress(GameState state)
        {
            if (state == null)
                return;
            // Never persist an identity-less or not-started state: it would masquerade as a save.
            if (string.IsNullOrEmpty(state.playerId) || state.step == QuestStep.None)
            {
                Debug.LogWarning("[Hegra] Refused to save a progress state without player id or step: " + Environment.StackTrace);
                return;
            }
            state.version = GameState.SaveVersion;
            WriteAtomic(ProgressPath, JsonUtility.ToJson(state, true));
        }

        public GameState LoadProgress()
        {
            var json = ReadOrNull(ProgressPath);
            if (string.IsNullOrEmpty(json))
                return null;
            try
            {
                var state = JsonUtility.FromJson<GameState>(json);
                if (state == null || string.IsNullOrEmpty(state.playerId) || state.step == QuestStep.None)
                    return null; // empty/placeholder file: treat as "no save"
                if (state.step != QuestStep.None && QuestFlow.Info(state.step) == null)
                    state.step = QuestStep.CampWelcome;
                return state;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Hegra] Progress file unreadable, ignoring it: {e.Message}");
                return null;
            }
        }

        public void DeleteProgress()
        {
            TryDelete(ProgressPath);
        }

        public void SaveSettings(SettingsData settings)
        {
            if (settings == null)
                return;
            settings.Clamp();
            WriteAtomic(SettingsPath, JsonUtility.ToJson(settings, true));
        }

        public SettingsData LoadSettings()
        {
            var json = ReadOrNull(SettingsPath);
            SettingsData s = null;
            if (!string.IsNullOrEmpty(json))
            {
                try { s = JsonUtility.FromJson<SettingsData>(json); }
                catch (Exception e) { Debug.LogWarning($"[Hegra] Settings unreadable, using defaults: {e.Message}"); }
            }
            s ??= new SettingsData();
            s.Clamp();
            return s;
        }

        void WriteAtomic(string path, string contents)
        {
            try
            {
                System.IO.Directory.CreateDirectory(m_Directory);
                var tmp = path + ".tmp";
                File.WriteAllText(tmp, contents);
                if (File.Exists(path))
                    File.Delete(path);
                File.Move(tmp, path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Hegra] Could not write {path}: {e.Message}");
            }
        }

        static string ReadOrNull(string path)
        {
            try
            {
                if (File.Exists(path))
                    return File.ReadAllText(path);
                // Recover from an interrupted swap.
                if (File.Exists(path + ".tmp"))
                    return File.ReadAllText(path + ".tmp");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Hegra] Could not read {path}: {e.Message}");
            }
            return null;
        }

        static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Hegra] Could not delete {path}: {e.Message}");
            }
        }
    }
}
