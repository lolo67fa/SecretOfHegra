using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace SecretOfHegra.EditorTools
{
    /// <summary>
    /// Generates lightweight PROCEDURAL PLACEHOLDER audio (synthesized; not recordings, not historical),
    /// creates the Audio Mixer (Master → Music / Ambience / SFX / Dialogue with exposed volumes) and the
    /// voice-over slot bank. Clips are only generated when missing, so replacing a WAV file with a real
    /// recording of the same name is preserved by later rebuilds.
    /// </summary>
    public static class HegraAudioAssets
    {
        public const string AudioDir = HegraKit.Root + "/Audio";
        public const string MixerPath = AudioDir + "/HegraMixer.mixer";
        const int Rate = 22050;

        public class Set
        {
            public AudioMixer mixer;
            public AudioMixerGroup musicGroup, ambienceGroup, sfxGroup, dialogueGroup;
            public readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
            public AudioClip Get(string n) => clips.TryGetValue(n, out var c) ? c : null;
        }

        public static Set EnsureAll()
        {
            HegraKit.EnsureFolder(AudioDir);
            var set = new Set();
            var gens = new (string name, float seconds, bool loop, Func<float[]> gen)[]
            {
                ("amb_wind", 10f, true, () => Wind(10f)),
                ("amb_fire", 8f, true, () => Fire(8f)),
                ("amb_water", 8f, true, () => Water(8f)),
                ("amb_market", 9.6f, true, () => MarketPercussion(9.6f)),
                ("mus_lyre", 19.2f, true, () => LyreMusic(19.2f)),
                ("mus_pipes", 16f, true, () => PipesMusic(16f)),
                ("mus_choir", 16f, true, () => Choir(16f)),
                ("sfx_footstep", 0.3f, false, () => Footstep()),
                ("sfx_carve", 0.25f, false, () => Carve()),
                ("sfx_door", 4f, false, () => StoneDoor(4f)),
                ("sfx_camel", 1.4f, false, () => Camel(1.4f)),
                ("sfx_coin", 0.8f, false, () => Coin()),
                ("sfx_click", 0.06f, false, () => Click()),
                ("sfx_chime", 1.8f, false, () => Chime()),
                ("sfx_grab", 0.12f, false, () => Grab()),
                ("sfx_kohl", 1f, false, () => Shimmer()),
                ("sfx_gate", 0.6f, false, () => Scrape(0.6f)),
                ("sfx_chest", 1.2f, false, () => Creak(1.2f)),
                ("sfx_fail", 0.6f, false, () => Fail()),
                // "babble" syllables for townspeople (pitched per speaker at runtime) + a soft crowd murmur
                ("vo_syl_0", 0.14f, false, () => Syllable(0, 0.14f)),
                ("vo_syl_1", 0.12f, false, () => Syllable(1, 0.12f)),
                ("vo_syl_2", 0.13f, false, () => Syllable(2, 0.13f)),
                ("vo_syl_3", 0.11f, false, () => Syllable(3, 0.11f)),
                ("vo_syl_4", 0.15f, false, () => Syllable(4, 0.15f)),
                ("amb_murmur", 12f, true, () => Murmur(12f)),
            };
            foreach (var g in gens)
            {
                var path = $"{AudioDir}/{g.name}.wav";
                if (!File.Exists(path))
                {
                    var data = g.gen();
                    if (g.loop) data = MakeSeamless(data, (int)(Rate * 0.4f));
                    Normalize(data, g.loop ? 0.7f : 0.85f);
                    WriteWav(path, data);
                    AssetDatabase.ImportAsset(path);
                    var imp = (AudioImporter)AssetImporter.GetAtPath(path);
                    imp.forceToMono = true;
                    var s = imp.defaultSampleSettings;
                    // loops (music/ambience) stream; short one-shots decompress up front; all preload (no first-play hitch)
                    s.loadType = g.loop ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                    s.preloadAudioData = true;
                    s.compressionFormat = AudioCompressionFormat.Vorbis;
                    s.quality = 0.45f;
                    imp.defaultSampleSettings = s;
                    imp.SaveAndReimport();
                }
                set.clips[g.name] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
            // made by Tools/Audio/hegra_audio.py (numpy/scipy synthesis); loaded when present
            foreach (var (name, music) in new[] { ("amb_crickets", false), ("amb_cloth", false), ("mus_hijaz", true), ("mus_legend", true),
                         ("sting_legend", false), ("sting_reveal", false), ("ui_open", false), ("ui_confirm", false) })
            {
                var path = $"{AudioDir}/{name}.wav";
                if (!File.Exists(path)) continue;
                AssetDatabase.ImportAsset(path);
                var imp = (AudioImporter)AssetImporter.GetAtPath(path);
                bool loop = name.StartsWith("amb_") || music;
                var s = imp.defaultSampleSettings;
                if (imp.forceToMono == music || s.loadType != (loop ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad))
                {
                    imp.forceToMono = !music;   // music keeps its stereo width
                    s.loadType = loop ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                    s.preloadAudioData = true;
                    s.compressionFormat = AudioCompressionFormat.Vorbis;
                    s.quality = music ? 0.5f : 0.45f;
                    imp.defaultSampleSettings = s;
                    imp.SaveAndReimport();
                }
                set.clips[name] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
            EnsureMixer(set);
            return set;
        }

        // ───────────── mixer ─────────────

        static void EnsureMixer(Set set)
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (!mixer)
            {
                try { CreateMixer(); }
                catch (Exception e) { Debug.LogError("[Hegra] Could not create the Audio Mixer automatically: " + (e.InnerException ?? e)); }
                mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            }
            set.mixer = mixer;
            if (!mixer) return;
            AudioMixerGroup Find(string n) => mixer.FindMatchingGroups(n).FirstOrDefault(g => g.name == n);
            set.musicGroup = Find("Music");
            set.ambienceGroup = Find("Ambience");
            set.sfxGroup = Find("SFX");
            set.dialogueGroup = Find("Dialogue");
        }

        static void CreateMixer()
        {
            var asm = typeof(Editor).Assembly;
            var ctrlType = asm.GetType("UnityEditor.Audio.AudioMixerController");
            var groupType = asm.GetType("UnityEditor.Audio.AudioMixerGroupController");
            var pathType = asm.GetType("UnityEditor.Audio.AudioGroupParameterPath");
            const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            var ctrl = ctrlType.GetMethod("CreateMixerControllerAtPath", All).Invoke(null, new object[] { MixerPath });
            var master = ctrlType.GetProperty("masterGroup", All).GetValue(ctrl);
            var createGroup = ctrlType.GetMethod("CreateNewGroup", All);
            var addChild = ctrlType.GetMethod("AddChildToParent", All);
            var addToView = ctrlType.GetMethod("AddGroupToCurrentView", All);
            var addExposed = ctrlType.GetMethod("AddExposedParameter", All);
            var getVolGuid = groupType.GetMethod("GetGUIDForVolume", All);
            var exposedProp = ctrlType.GetProperty("exposedParameters", All);

            var names = new[] { "Music", "Ambience", "SFX", "Dialogue" };
            var paramNames = new List<string>();
            foreach (var n in names)
            {
                var g = createGroup.Invoke(ctrl, new object[] { n, false });
                addChild.Invoke(ctrl, new[] { g, master });
                try { addToView?.Invoke(ctrl, new[] { g }); } catch (Exception) { /* view bookkeeping only */ }
                var guid = getVolGuid.Invoke(g, null);
                var path = Activator.CreateInstance(pathType, All, null, new[] { g, guid }, null);
                addExposed.Invoke(ctrl, new[] { path });
                paramNames.Add(n == "SFX" ? "SfxVolume" : n + "Volume");
            }

            // rename exposed parameters (order follows insertion; match by group via path resolution is unnecessary)
            var arr = (Array)exposedProp.GetValue(ctrl);
            var elemType = arr.GetType().GetElementType();
            var nameField = elemType.GetField("name", All);
            var guidField = elemType.GetField("guid", All);
            // Map each exposed GUID to its group's name using the groups' volume GUIDs.
            var groups = (System.Collections.IEnumerable)ctrlType.GetMethod("GetAllAudioGroupsSlow", All).Invoke(ctrl, null);
            var guidToName = new Dictionary<string, string>();
            foreach (var g in groups)
            {
                var gname = ((UnityEngine.Object)g).name;
                guidToName[getVolGuid.Invoke(g, null).ToString()] = gname == "SFX" ? "SfxVolume" : gname + "Volume";
            }
            for (int i = 0; i < arr.Length; i++)
            {
                var e = arr.GetValue(i);
                var gid = guidField.GetValue(e).ToString();
                if (guidToName.TryGetValue(gid, out var pn)) nameField.SetValue(e, pn);
                arr.SetValue(e, i);
            }
            exposedProp.SetValue(ctrl, arr);
            EditorUtility.SetDirty((UnityEngine.Object)ctrl);
            AssetDatabase.SaveAssets();
        }

        // the Hijaz oud piece plays for the whole journey; the lyre theme stays in the project as an alternative
        public const string JourneyMusicClip = "mus_hijaz";
        public const string LegendMusicClip = "mus_legend";

        public static void Configure(AudioDirector director, Set set)
        {
            var so = new SerializedObject(director);
            so.FindProperty("m_Mixer").objectReferenceValue = set.mixer;
            so.FindProperty("m_MusicGroup").objectReferenceValue = set.musicGroup;
            so.FindProperty("m_AmbienceGroup").objectReferenceValue = set.ambienceGroup;
            so.FindProperty("m_SfxGroup").objectReferenceValue = set.sfxGroup;
            so.FindProperty("m_DialogueGroup").objectReferenceValue = set.dialogueGroup;
            so.FindProperty("m_WindLoop").objectReferenceValue = set.Get("amb_wind");
            // the lyre theme (formerly the Theater / Qasr al-Bint zone music) now plays for the whole journey
            so.FindProperty("m_JourneyMusic").objectReferenceValue = set.Get(JourneyMusicClip) ?? set.Get("mus_lyre");
            so.FindProperty("m_LegendMusic").objectReferenceValue = set.Get(LegendMusicClip);
            var list = so.FindProperty("m_Sfx");
            var map = new (Sfx id, string clip)[]
            {
                (Sfx.UiClick, "sfx_click"), (Sfx.SignChime, "sfx_chime"), (Sfx.Coin, "sfx_coin"), (Sfx.Carve, "sfx_carve"),
                (Sfx.StoneDoor, "sfx_door"), (Sfx.Footstep, "sfx_footstep"), (Sfx.Camel, "sfx_camel"), (Sfx.Grab, "sfx_grab"),
                (Sfx.KohlApply, "sfx_kohl"), (Sfx.WaterGate, "sfx_gate"), (Sfx.Chest, "sfx_chest"), (Sfx.Fail, "sfx_fail"),
                (Sfx.LegendSting, "sting_legend"), (Sfx.RevealSting, "sting_reveal"), (Sfx.UiOpen, "ui_open"), (Sfx.UiConfirm, "ui_confirm"),
            };
            list.arraySize = map.Length;
            for (int i = 0; i < map.Length; i++)
            {
                var e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("id").enumValueIndex = (int)map[i].id;
                e.FindPropertyRelative("clip").objectReferenceValue = set.Get(map[i].clip);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>One empty AR/EN voice-over slot per dialogue line (fill with recordings later).</summary>
        public static VoiceoverBank EnsureVoiceoverBank()
        {
            var path = AudioDir + "/VoiceoverBank.asset";
            var bank = AssetDatabase.LoadAssetAtPath<VoiceoverBank>(path);
            if (!bank)
            {
                bank = ScriptableObject.CreateInstance<VoiceoverBank>();
                AssetDatabase.CreateAsset(bank, path);
            }
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(HegraKit.Root + "/Data/dialogue.json");
            if (json)
            {
                var db = JsonUtility.FromJson<DialogueKeys>(json.text);
                var have = new HashSet<string>(bank.slots.Select(s => s.key));
                if (db?.entries != null)
                    foreach (var e in db.entries)
                        if (!have.Contains(e.key)) bank.slots.Add(new VoiceoverBank.Slot { key = e.key });
            }
            EditorUtility.SetDirty(bank);
            return bank;
        }

        [Serializable] class DialogueKeys { public Loc.Entry[] entries; }

        // ───────────── synthesis helpers ─────────────

        static float[] Buf(float seconds) => new float[Mathf.CeilToInt(seconds * Rate)];

        static float[] MakeSeamless(float[] d, int fade)
        {
            // Crossfade the tail into the head and drop the tail: the loop point is then continuous
            // (last output sample flows into the first one exactly as in the original signal).
            fade = Mathf.Min(fade, d.Length / 4);
            int len = d.Length - fade;
            var o = new float[len];
            Array.Copy(d, o, len);
            for (int i = 0; i < fade; i++)
            {
                float t = (float)i / fade;
                o[i] = d[i] * t + d[len + i] * (1f - t);
            }
            return o;
        }

        static void Normalize(float[] d, float peak)
        {
            float m = 0.0001f;
            foreach (var s in d) m = Mathf.Max(m, Mathf.Abs(s));
            float k = peak / m;
            for (int i = 0; i < d.Length; i++) d[i] *= k;
        }

        static void WriteWav(string path, float[] data)
        {
            using var fs = new FileStream(path, FileMode.Create);
            using var w = new BinaryWriter(fs);
            int bytes = data.Length * 2;
            w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + bytes);
            w.Write(new[] { 'W', 'A', 'V', 'E' }); w.Write(new[] { 'f', 'm', 't', ' ' });
            w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(bytes);
            foreach (var s in data) w.Write((short)Mathf.Clamp(s * 32767f, -32768f, 32767f));
        }

        class Rng
        {
            uint s;
            public Rng(uint seed) { s = seed * 747796405u + 2891336453u; }
            public float Next() { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return (s & 0xFFFFFF) / (float)0xFFFFFF * 2f - 1f; }
        }

        struct Biquad
        {
            float a0, a1, a2, b1, b2, z1, z2;
            public static Biquad BandPass(float f, float q)
            {
                float w = 2f * Mathf.PI * f / Rate, alpha = Mathf.Sin(w) / (2f * q), c = Mathf.Cos(w), n = 1f + alpha;
                return new Biquad { a0 = alpha / n, a1 = 0, a2 = -alpha / n, b1 = -2f * c / n, b2 = (1f - alpha) / n };
            }
            public static Biquad LowPass(float f, float q = 0.707f)
            {
                float w = 2f * Mathf.PI * f / Rate, alpha = Mathf.Sin(w) / (2f * q), c = Mathf.Cos(w), n = 1f + alpha;
                return new Biquad { a0 = (1 - c) / 2f / n, a1 = (1 - c) / n, a2 = (1 - c) / 2f / n, b1 = -2f * c / n, b2 = (1f - alpha) / n };
            }
            public float Process(float x)
            {
                float y = a0 * x + z1;
                z1 = a1 * x - b1 * y + z2;
                z2 = a2 * x - b2 * y;
                return y;
            }
        }

        static float Env(float t, float attack, float decay) => t < attack ? t / attack : Mathf.Exp(-(t - attack) / decay);

        // ───────────── generators ─────────────

        static float[] Wind(float sec)
        {
            var d = Buf(sec); var r = new Rng(1); float b = 0;
            var bp = Biquad.BandPass(400f, 0.6f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                b = b * 0.985f + r.Next() * 0.05f;
                float gust = 0.55f + 0.45f * Mathf.Sin(t * 0.6f) * Mathf.Sin(t * 0.23f + 1f);
                d[i] = (b * 0.8f + bp.Process(r.Next()) * 0.25f) * gust;
            }
            return d;
        }

        static float[] Fire(float sec)
        {
            var d = Buf(sec); var r = new Rng(2); var lp = Biquad.LowPass(900f);
            for (int i = 0; i < d.Length; i++) d[i] = lp.Process(r.Next()) * 0.35f;
            for (int k = 0; k < sec * 9; k++)
            {
                int at = (int)((r.Next() * 0.5f + 0.5f) * (d.Length - 600));
                float amp = 0.3f + 0.7f * (r.Next() * 0.5f + 0.5f);
                for (int j = 0; j < 500; j++) d[at + j] += r.Next() * amp * Mathf.Exp(-j / 60f);
            }
            return d;
        }

        static float[] Water(float sec)
        {
            var d = Buf(sec); var r = new Rng(3); var bp = Biquad.BandPass(1200f, 0.8f); var lp = Biquad.LowPass(500f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                d[i] = bp.Process(r.Next()) * (0.4f + 0.2f * Mathf.Sin(t * 3.1f)) + lp.Process(r.Next()) * 0.3f;
            }
            for (int k = 0; k < sec * 6; k++)
            {
                int at = (int)((r.Next() * 0.5f + 0.5f) * (d.Length - 3000));
                float f = 500f + (r.Next() * 0.5f + 0.5f) * 900f;
                for (int j = 0; j < 2500; j++)
                {
                    float tt = j / (float)Rate;
                    d[at + j] += Mathf.Sin(2f * Mathf.PI * f * (1f + tt * 3f) * tt) * Mathf.Exp(-tt * 40f) * 0.25f;
                }
            }
            return d;
        }

        static void Drum(float[] d, int at, float freq, float amp, Rng r)
        {
            for (int j = 0; j < Rate * 0.4f && at + j < d.Length; j++)
            {
                float t = j / (float)Rate;
                float f = freq * (1f + 1.5f * Mathf.Exp(-t * 30f));
                d[at + j] += (Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 9f) + r.Next() * 0.2f * Mathf.Exp(-t * 60f)) * amp;
            }
        }

        static void Clap(float[] d, int at, float amp, Rng r)
        {
            var bp = Biquad.BandPass(1500f, 1.2f);
            for (int j = 0; j < Rate * 0.12f && at + j < d.Length; j++)
            {
                float t = j / (float)Rate;
                float e = Mathf.Exp(-t * 35f) * (t < 0.01f || (t > 0.012f && t < 0.02f) || t > 0.025f ? 1f : 0.3f);
                d[at + j] += bp.Process(r.Next()) * e * amp;
            }
        }

        static void Cymbal(float[] d, int at, float amp, Rng r)
        {
            float prev = 0;
            for (int j = 0; j < Rate * 0.6f && at + j < d.Length; j++)
            {
                float t = j / (float)Rate;
                float n = r.Next();
                float hp = n - prev; prev = n;
                d[at + j] += hp * Mathf.Exp(-t * 6f) * amp;
            }
        }

        static float[] MarketPercussion(float sec)
        {
            var d = Buf(sec); var r = new Rng(4);
            float beat = 60f / 100f; int beats = Mathf.RoundToInt(sec / beat);
            for (int b = 0; b < beats; b++)
            {
                int at = (int)(b * beat * Rate);
                int bar = b % 8;
                if (bar == 0 || bar == 3 || bar == 4 || bar == 6) Drum(d, at, 85f, 1f, r);
                if (bar == 2 || bar == 5 || bar == 7) Drum(d, at, 190f, 0.6f, r);
                if (b % 2 == 1) Clap(d, at + (int)(beat * Rate * 0.5f), 0.5f, r);
                if (bar == 0) Cymbal(d, at, 0.25f, r);
            }
            return d;
        }

        // Pitch set loosely inspired by a Hijaz-like scale (illustrative only; not a historical reconstruction).
        static readonly float[] Scale = { 293.66f, 311.13f, 369.99f, 392f, 440f, 466.16f, 554.37f, 587.33f };

        static void Pluck(float[] d, int at, float f, float amp, Rng r, float sec = 1.6f)
        {
            int n = Mathf.Max(2, (int)(Rate / f));
            var ring = new float[n];
            for (int i = 0; i < n; i++) ring[i] = r.Next();
            int idx = 0;
            for (int j = 0; j < Rate * sec && at + j < d.Length; j++)
            {
                float v = ring[idx];
                int nx = (idx + 1) % n;
                ring[idx] = (ring[idx] + ring[nx]) * 0.4985f;
                idx = nx;
                d[at + j] += v * amp;
            }
        }

        static float[] LyreMusic(float sec)
        {
            var d = Buf(sec); var r = new Rng(5);
            int[] melody = { 0, 2, 3, 4, 3, 2, 1, 0, 4, 5, 6, 7, 6, 4, 3, 2, 0, 3, 2, 1, 0, 4, 2, 0 };
            float step = sec / melody.Length;
            for (int i = 0; i < melody.Length; i++)
            {
                int at = (int)(i * step * Rate);
                Pluck(d, at, Scale[melody[i]] * 0.5f, 0.6f, r);
                if (i % 4 == 0) Pluck(d, at, Scale[0] * 0.25f, 0.45f, r, 2.4f);
            }
            return d;
        }

        static float Reed(float phase)
        {
            // bright, nasal waveform: sum of odd/even harmonics with falling weights
            float s = 0;
            for (int h = 1; h <= 7; h++) s += Mathf.Sin(phase * h) / (h * (h % 2 == 0 ? 1.6f : 1f));
            return s * 0.35f;
        }

        static float[] PipesMusic(float sec)
        {
            var d = Buf(sec);
            int[] melody = { 4, 5, 4, 3, 2, 3, 4, 2, 1, 2, 3, 2, 1, 0, 1, 0 };
            float noteLen = sec / melody.Length;
            float ph1 = 0, ph2 = 0;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                int n = Mathf.Min(melody.Length - 1, (int)(t / noteLen));
                float nt = t - n * noteLen;
                float vib = 1f + 0.006f * Mathf.Sin(t * 2f * Mathf.PI * 5.5f);
                float f = Scale[melody[n]] * vib;
                ph1 += 2f * Mathf.PI * f / Rate;
                ph2 += 2f * Mathf.PI * Scale[0] * 0.5f / Rate; // drone pipe
                float env = Mathf.Min(1f, nt * 20f) * Mathf.Min(1f, (noteLen - nt) * 20f);
                d[i] = Reed(ph1) * env * 0.7f + Reed(ph2) * 0.35f;
            }
            var lp = Biquad.LowPass(2800f);
            for (int i = 0; i < d.Length; i++) d[i] = lp.Process(d[i]);
            return d;
        }

        static float[] Choir(float sec)
        {
            var d = Buf(sec);
            float[] chord = { 146.83f, 220f, 293.66f, 349.23f };
            var f1 = Biquad.BandPass(700f, 3f); var f2 = Biquad.BandPass(1150f, 4f); var f3 = Biquad.BandPass(2600f, 6f);
            var phases = new float[chord.Length * 2];
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float src = 0;
                for (int c = 0; c < chord.Length; c++)
                    for (int v = 0; v < 2; v++)
                    {
                        int k = c * 2 + v;
                        float f = chord[c] * (1f + (v == 0 ? -0.004f : 0.004f)) * (1f + 0.004f * Mathf.Sin(t * 5f + k));
                        phases[k] += 2f * Mathf.PI * f / Rate;
                        float saw = 0;
                        for (int h = 1; h <= 10; h++) saw += Mathf.Sin(phases[k] * h) / h;
                        src += saw;
                    }
                float swell = 0.6f + 0.4f * Mathf.Sin(t * 2f * Mathf.PI / sec);
                d[i] = (f1.Process(src) + f2.Process(src) * 0.6f + f3.Process(src) * 0.25f) * swell;
            }
            return d;
        }

        static float[] Footstep()
        {
            var d = Buf(0.3f); var r = new Rng(6); var lp = Biquad.LowPass(1400f);
            for (int i = 0; i < d.Length; i++) { float t = (float)i / Rate; d[i] = lp.Process(r.Next()) * Env(t, 0.01f, 0.07f); }
            return d;
        }

        static float[] Carve()
        {
            var d = Buf(0.25f); var r = new Rng(7); var bp = Biquad.BandPass(3200f, 2f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                d[i] = bp.Process(r.Next()) * Env(t, 0.001f, 0.03f) + Mathf.Sin(2f * Mathf.PI * 1800f * t) * Env(t, 0.001f, 0.015f) * 0.5f;
            }
            return d;
        }

        static float[] StoneDoor(float sec)
        {
            var d = Buf(sec); var r = new Rng(8); var lp = Biquad.LowPass(220f); var bp = Biquad.BandPass(700f, 1.5f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float env = Mathf.Min(1f, t * 2f) * Mathf.Min(1f, (sec - t) * 1.5f);
                float grind = 0.6f + 0.4f * Mathf.Sin(t * 37f) * Mathf.Sin(t * 11f);
                d[i] = (lp.Process(r.Next()) * 1.5f + bp.Process(r.Next()) * 0.3f * grind) * env;
            }
            return d;
        }

        static float[] Camel(float sec)
        {
            var d = Buf(sec); var f1 = Biquad.BandPass(450f, 2f); var f2 = Biquad.BandPass(900f, 3f); float ph = 0;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float f = 85f + 25f * Mathf.Sin(t * 3f) + 6f * Mathf.Sin(t * 37f);
                ph += 2f * Mathf.PI * f / Rate;
                float saw = 0;
                for (int h = 1; h < 14; h++) saw += Mathf.Sin(ph * h) / h;
                float env = Mathf.Min(1f, t * 6f) * Mathf.Min(1f, (sec - t) * 3f);
                d[i] = (f1.Process(saw) + f2.Process(saw) * 0.5f) * env;
            }
            return d;
        }

        static float[] Coin()
        {
            var d = Buf(0.8f); float[] fs = { 2400f, 3710f, 5230f, 6890f };
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate; float s = 0;
                for (int k = 0; k < fs.Length; k++) s += Mathf.Sin(2f * Mathf.PI * fs[k] * t) * Mathf.Exp(-t * (5f + k * 3f)) / (k + 1);
                d[i] = s;
            }
            return d;
        }

        static float[] Click()
        {
            var d = Buf(0.06f);
            for (int i = 0; i < d.Length; i++) { float t = (float)i / Rate; d[i] = Mathf.Sin(2f * Mathf.PI * 1300f * t) * Env(t, 0.001f, 0.012f); }
            return d;
        }

        static float[] Chime()
        {
            var d = Buf(1.8f); int[] notes = { 0, 2, 4, 7 };
            for (int n = 0; n < notes.Length; n++)
            {
                int at = (int)(n * 0.12f * Rate);
                float f = Scale[notes[n]] * 2f;
                for (int j = 0; at + j < d.Length; j++)
                {
                    float t = j / (float)Rate;
                    d[at + j] += (Mathf.Sin(2f * Mathf.PI * f * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * f * 2.76f * t)) * Mathf.Exp(-t * 3f) * 0.5f;
                }
            }
            return d;
        }

        static float[] Grab()
        {
            var d = Buf(0.12f); var r = new Rng(9); var lp = Biquad.LowPass(600f);
            for (int i = 0; i < d.Length; i++) { float t = (float)i / Rate; d[i] = lp.Process(r.Next()) * Env(t, 0.003f, 0.03f); }
            return d;
        }

        static float[] Shimmer()
        {
            var d = Buf(1f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                d[i] = (Mathf.Sin(2f * Mathf.PI * 1760f * t) + Mathf.Sin(2f * Mathf.PI * 2217f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 2637f * t) * 0.4f)
                       * Mathf.Sin(Mathf.PI * t) * 0.3f * (0.7f + 0.3f * Mathf.Sin(t * 60f));
            }
            return d;
        }

        static float[] Scrape(float sec)
        {
            var d = Buf(sec); var r = new Rng(10); var bp = Biquad.BandPass(500f, 1f);
            for (int i = 0; i < d.Length; i++) { float t = (float)i / Rate; d[i] = bp.Process(r.Next()) * Mathf.Sin(Mathf.PI * t / sec) * (0.7f + 0.3f * Mathf.Sin(t * 90f)); }
            return d;
        }

        static float[] Creak(float sec)
        {
            var d = Buf(sec); float ph = 0; var bp = Biquad.BandPass(800f, 4f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float f = 120f + 60f * t / sec;
                ph += 2f * Mathf.PI * f / Rate;
                float pulse = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(ph)), 12f);
                d[i] = bp.Process(pulse) * Mathf.Sin(Mathf.PI * t / sec);
            }
            return d;
        }

        static float[] Fail()
        {
            var d = Buf(0.6f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float f = t < 0.25f ? 330f : 262f;
                d[i] = Mathf.Sin(2f * Mathf.PI * f * t) * Env(t % 0.25f, 0.01f, 0.12f) * 0.6f;
            }
            return d;
        }

        // ───────────── babble voice ─────────────

        // Rough vowel formants (F1, F2) in Hz for a, e, i, o, u.
        static readonly (float f1, float f2)[] s_Vowels = { (750, 1250), (500, 1800), (320, 2300), (500, 900), (350, 750) };

        /// <summary>One short sung syllable: a soft consonant tick into a formant-filtered vowel (unintelligible on purpose).</summary>
        static float[] Syllable(int vowel, float sec)
        {
            var d = Buf(sec);
            var r = new Rng((uint)(101 + vowel * 17));
            var (f1, f2) = s_Vowels[vowel % s_Vowels.Length];
            var b1 = Biquad.BandPass(f1, 4f);
            var b2 = Biquad.BandPass(f2, 6f);
            float phase = 0f;
            const float f0 = 220f; // neutral pitch; AudioSource.pitch shifts it per speaker
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float f = f0 * (1f + 0.08f * Mathf.Sin(t / sec * Mathf.PI)); // small rise-fall
                phase += f / Rate;
                float saw = (phase % 1f) * 2f - 1f;
                float tick = t < 0.012f ? r.Next() * (1f - t / 0.012f) * 0.6f : 0f;
                float v = b1.Process(saw) * 0.9f + b2.Process(saw) * 0.5f + tick;
                float env = Mathf.Min(1f, t / 0.015f) * Mathf.Clamp01((sec - t) / 0.04f);
                d[i] = v * env;
            }
            return d;
        }

        /// <summary>Distant crowd: many quiet, low-passed babble syllables at random pitches and times.</summary>
        static float[] Murmur(float sec)
        {
            var d = Buf(sec);
            var r = new Rng(77);
            var syl = new float[5][];
            for (int k = 0; k < 5; k++) syl[k] = Syllable(k, 0.12f + k * 0.01f);
            int voices = (int)(sec * 18f);
            for (int n = 0; n < voices; n++)
            {
                var src = syl[(int)((r.Next() * 0.5f + 0.5f) * 4.99f)];
                float pitch = 0.7f + (r.Next() * 0.5f + 0.5f) * 0.8f;
                int at = (int)((r.Next() * 0.5f + 0.5f) * (d.Length - 1));
                float amp = 0.12f + (r.Next() * 0.5f + 0.5f) * 0.18f;
                for (int i = 0; ; i++)
                {
                    int si = (int)(i * pitch);
                    if (si >= src.Length) break;
                    d[(at + i) % d.Length] += src[si] * amp;
                }
            }
            var lp = Biquad.LowPass(1400f);
            for (int i = 0; i < d.Length; i++) d[i] = lp.Process(d[i]);
            return d;
        }
    }
}
