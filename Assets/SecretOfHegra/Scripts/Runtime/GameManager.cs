using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace SecretOfHegra
{
    /// <summary>
    /// Owns the playthrough: state + saves, the progression state machine, the night clock (paused in menus,
    /// on focus loss and when the headset is off), hints, the late-arrival assisted route, and settings.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public enum StartMode { Menu, NewGame, Continue }

        /// <summary>What to do after the next scene load (set before reloading for New Game / Continue).</summary>
        public static StartMode PendingStart = StartMode.Menu;

        [Header("Data")]
        [SerializeField] TextAsset m_UiStrings;
        [SerializeField] TextAsset m_DialogueJson;
        [SerializeField] TextAsset m_NabataeanMap;
        [SerializeField] TMP_FontAsset m_Font;
        [SerializeField] TMP_FontAsset m_NabataeanFont;

        [Header("Systems")]
        [SerializeField] PlayerRig m_Rig;
        [SerializeField] DialogueRunner m_Dialogue;
        [SerializeField] AudioDirector m_Audio;
        [SerializeField] MenuSystem m_Menus;
        [SerializeField] MoonController m_Moon;
        [SerializeField] BodyAnchor m_Body;
        [SerializeField] HegraSocket m_BeltSocket;
        [SerializeField] HegraSocket m_SatchelSocket;
        [SerializeField] HegraItem m_Lamp;
        [SerializeField] List<StopBase> m_Stops = new List<StopBase>();

        [Header("Timing")]
        [SerializeField] float m_TargetDurationSeconds = 900f;
        [SerializeField] float m_LateAssistSeconds = 750f;
        [SerializeField] float m_ReofferSeconds = 120f;
        [SerializeField] float m_IdleHintSeconds = 75f;

        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; }
        public SettingsData Settings { get; private set; }
        public SaveSystem Saves { get; private set; }
        public PlayerRig Rig => m_Rig;
        public DialogueRunner Dialogue => m_Dialogue;
        public AudioDirector Audio => m_Audio;
        public MenuSystem Menus => m_Menus;
        public ObjectivePanel Objective { get; private set; }
        public SubtitleDisplay Subtitles { get; private set; }
        public NabataeanConverter Nabataean { get; private set; }
        public TMP_FontAsset NabataeanFont => m_NabataeanFont;
        public TMP_FontAsset Font => m_Font;
        public HegraSocket BeltSocket => m_BeltSocket;
        public HegraSocket SatchelSocket => m_SatchelSocket;
        public HegraItem Lamp => m_Lamp;
        public BodyAnchor Body => m_Body;
        public IReadOnlyList<StopBase> Stops => m_Stops;
        public HegraInput Input { get; private set; }

        public bool InGame { get; private set; }
        public bool MenuOpen { get; set; }
        public bool AppFocused { get; private set; } = true;
        public bool HeadsetPresent { get; private set; } = true;
        public bool IsTimerPaused => !InGame || MenuOpen || !AppFocused || !HeadsetPresent || State == null || State.step == QuestStep.Complete;
        public float NightProgress => State == null ? 0f : Mathf.Clamp01(State.museumMode ? StageProgress() : Mathf.Max(State.elapsedSeconds / m_TargetDurationSeconds, StageProgress() * 0.85f));
        public bool AssistOfferOpen { get; private set; }

        public event Action<QuestStep> StepEntered;
        public event Action StateChanged;
        public event Action<SettingsData> SettingsChanged;
        public event Action<SignId> SignGranted;

        StopBase m_ActiveStop;
        float m_StepTimer;
        float m_PresenceTimer;
        float m_SaveTimer;
        bool m_AssistRunning;

        // VR focus/presence: raw values from the callbacks and the presence sensor; the public flags change only
        // after the player has been away for PauseDebounceSeconds (Guardian, system overlay and sensor blips are
        // ignored) and recover immediately on return, so the game never stays paused after resuming.
        const float PauseDebounceSeconds = 1f;
        bool m_RawFocus = true, m_RawPresent = true, m_SeenPresent;
        float m_AwaySince = -1f;

        void Awake()
        {
            Instance = this;
            Saves = new SaveSystem();
            Loc.Clear();
            if (m_UiStrings) Loc.LoadTable(m_UiStrings.text);
            Loc.Font = m_Font;
            Nabataean = m_NabataeanMap ? NabataeanConverter.FromJson(m_NabataeanMap.text) : new NabataeanConverter();
            if (m_NabataeanFont)
                Nabataean.hasGlyph = cp =>
                {
                    if (m_NabataeanFont.characterLookupTable.ContainsKey((uint)cp)) return true;
                    m_NabataeanFont.TryAddCharacters(new[] { (uint)cp }, out uint[] missing);
                    return missing == null || missing.Length == 0;
                };
            Settings = Saves.LoadSettings();
            Loc.SetLanguage(Settings.language);
            Input = new HegraInput();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Input?.Dispose();
        }

        void OnEnable() => Input?.Enable();
        void OnDisable() => Input?.Disable();

        void Start()
        {
            var cam = m_Rig ? m_Rig.Camera : Camera.main;
            Subtitles = SubtitleDisplay.Create(m_Rig.Head, cam);
            Objective = ObjectivePanel.Create(m_Rig.Head, cam);

            m_Dialogue.Database = DialogueDatabase.FromJson(m_Dialogue_Text());
            m_Dialogue.Subtitles = Subtitles;
            m_Dialogue.IsPaused = () => MenuOpen || !AppFocused || !HeadsetPresent;
            if (m_Audio)
            {
                m_Audio.Listener = m_Rig.Head;
                if (m_Dialogue.VoiceSource) m_Dialogue.VoiceSource.outputAudioMixerGroup = m_Audio.DialogueGroup;
            }

            m_LampCarry = m_Rig ? m_Rig.GetComponent<LampHandCarry>() : null;
            foreach (var s in m_Stops) if (s) s.Initialize();
            if (m_Menus) m_Menus.Initialize(this);

            ApplySettings(Settings, false);

            var mode = PendingStart;
            PendingStart = StartMode.Menu;
            if (mode == StartMode.NewGame) StartCoroutine(BeginGame(true));
            else if (mode == StartMode.Continue && Saves.HasProgress()) StartCoroutine(BeginGame(false));
            else StartCoroutine(ShowMainMenuRoutine());
        }

        string m_Dialogue_Text() => m_DialogueJson ? m_DialogueJson.text : "";

        /// <summary>Editor builder hook: assigns the data assets.</summary>
        public void ConfigureData(TextAsset ui, TextAsset dialogue, TextAsset nabataean, TMP_FontAsset font, TMP_FontAsset nabataeanFont)
        {
            m_UiStrings = ui; m_DialogueJson = dialogue; m_NabataeanMap = nabataean;
            m_Font = font; m_NabataeanFont = nabataeanFont;
        }

        public void ConfigureSystems(PlayerRig rig, DialogueRunner dialogue, AudioDirector audio, MenuSystem menus, MoonController moon,
            BodyAnchor body, HegraSocket belt, HegraSocket satchel, HegraItem lamp, List<StopBase> stops)
        {
            m_Rig = rig; m_Dialogue = dialogue; m_Audio = audio; m_Menus = menus; m_Moon = moon; m_Body = body;
            m_BeltSocket = belt; m_SatchelSocket = satchel; m_Lamp = lamp; m_Stops = stops;
        }

        public bool DataConfigured => m_UiStrings && m_DialogueJson && m_NabataeanMap && m_Font;
        public bool SystemsConfigured => m_Rig && m_Dialogue && m_Menus && m_Body && m_BeltSocket && m_SatchelSocket && m_Lamp && m_Stops.Count > 0;

        // ───────────────────────── Game flow ─────────────────────────

        IEnumerator ShowMainMenuRoutine()
        {
            InGame = false;
            var camp = GetStop(StopId.Camp);
            if (camp && camp.spawnPoint) m_Rig.PlaceAt(camp.spawnPoint);
            CarryLampInHand();
            yield return null;
            m_Menus.ShowMain();
            yield return m_Rig.Fader.FadeTo(0f, 1.2f);
        }

        /// <summary>Main-menu entry points. Starting from a fresh load avoids leftover world state.</summary>
        public void RequestNewGame()
        {
            if (!InGame && State == null) { m_Menus.CloseAll(); StartCoroutine(BeginGame(true)); }
            else ReloadWith(StartMode.NewGame);
        }

        public void RequestContinue()
        {
            if (!Saves.HasProgress()) return;
            if (!InGame && State == null) { m_Menus.CloseAll(); StartCoroutine(BeginGame(false)); }
            else ReloadWith(StartMode.Continue);
        }

        public void RequestReturnToMenu()
        {
            SaveNow();
            ReloadWith(StartMode.Menu);
        }

        public void RequestRestart()
        {
            Saves.DeleteProgress();
            ReloadWith(StartMode.NewGame);
        }

        void ReloadWith(StartMode mode)
        {
            StartCoroutine(ReloadRoutine(mode));
        }

        IEnumerator ReloadRoutine(StartMode mode)
        {
            m_Menus.CloseAll();
            yield return m_Rig.Fader.FadeTo(1f);
            PendingStart = mode;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0 ? SceneManager.GetActiveScene().buildIndex : 0);
        }

        IEnumerator BeginGame(bool fresh)
        {
            m_Rig.Fader.SetAlpha(1f);
            m_Menus.CloseAll();
            if (fresh)
            {
                Saves.DeleteProgress();
                State = GameState.CreateNew(Settings.museumMode);
                State.step = QuestFlow.FirstStep;
                SaveNow();
            }
            else
            {
                State = Saves.LoadProgress() ?? GameState.CreateNew(Settings.museumMode);
                if (State.step == QuestStep.None) State.step = QuestFlow.FirstStep;
            }

            var stop = GetStop(QuestFlow.StopOf(State.step));
            if (stop && stop.spawnPoint) m_Rig.PlaceAt(stop.spawnPoint);
            foreach (var s in m_Stops) if (s) s.RestoreState(State);
            RestoreInventory();
            yield return null;
            InGame = true;
            yield return m_Rig.Fader.FadeTo(0f, 1f);
            EnterStep(State.step);
        }

        void RestoreInventory()
        {
            if (m_Lamp)
            {
                // the player always has the lamp: in the dominant hand until it has been hung on the belt once
                // (old saves included: they never put it back on a table), afterwards on the belt
                State.lampAcquired = true;
                if (State.lampStoredOnce) PutLampOnBelt();
                else CarryLampInHand();
            }
            KohlEffect.SetApplied(State.kohlApplied, true);
        }

        LampHandCarry m_LampCarry;

        /// <summary>The lamp in the dominant hand (journey start, main menu, saves from before the belt lesson).</summary>
        public void CarryLampInHand()
        {
            if (m_LampCarry) m_LampCarry.Carry(Settings != null ? Settings.handedness : Handedness.Right);
            else if (m_Lamp && m_BeltSocket) m_BeltSocket.ForceInsert(m_Lamp);
        }

        /// <summary>The lamp on the belt hook (belt lesson done, "hang it for me", skip tutorial, assisted route).</summary>
        public void PutLampOnBelt()
        {
            if (m_LampCarry) m_LampCarry.StoreOnBelt();
            else if (m_Lamp && m_BeltSocket) m_BeltSocket.ForceInsert(m_Lamp);
        }

        // ───────────────────────── Progression ─────────────────────────

        public StopBase GetStop(StopId id)
        {
            foreach (var s in m_Stops) if (s && s.stopId == id) return s;
            return null;
        }

        /// <summary>Completes the current step if it matches and its condition holds.</summary>
        public bool Complete(QuestStep step)
        {
            if (State == null || !InGame) return false;
            if (!QuestFlow.TryComplete(State, step)) return false;
            QuestFlow.AutoAdvance(State);
            SaveNow();
            EnterStep(State.step);
            return true;
        }

        /// <summary>Stops call this after changing state flags so satisfied condition steps advance.</summary>
        public void NotifyStateChanged()
        {
            if (State == null) return;
            StateChanged?.Invoke();
            if (!InGame) { SaveNow(); return; }
            var before = State.step;
            if (QuestFlow.AutoAdvance(State) > 0)
            {
                SaveNow();
                EnterStep(State.step);
            }
            else if (before == State.step)
                SaveNow();
        }

        /// <summary>Accessible skip for the camp tutorial: grants the lessons and moves to the farewell.</summary>
        public void SkipTutorial()
        {
            if (State == null || !QuestFlow.SkipTutorial(State)) return;
            PutLampOnBelt();
            SaveNow();
            EnterStep(State.step);
        }

        public bool GrantSign(SignId id)
        {
            if (State == null || !State.GrantSign(id)) return false;
            HegraAudio.Play2D(Sfx.SignChime);
            SignGranted?.Invoke(id);
            m_Menus?.ShowToast("sign.granted." + id.ToString().ToLowerInvariant());
            NotifyStateChanged();
            return true;
        }

        void EnterStep(QuestStep step)
        {
            m_StepTimer = 0f;
            ObjectiveBeacon.Current = null;
            var stop = GetStop(QuestFlow.StopOf(step));
            if (m_ActiveStop && m_ActiveStop != stop) m_ActiveStop.OnStepExited(step);
            m_ActiveStop = stop;

            string objKey = "obj." + step;
            if (Loc.Has(objKey)) Objective.Show(objKey, Loc.Has("ctl." + step) ? "ctl." + step : null, DescribeControls);
            else Objective.Hide();

            StepEntered?.Invoke(step);
            if (step == QuestStep.Complete)
            {
                Objective.Hide();
                m_Menus.ShowEnding();
                return;
            }
            if (stop) stop.OnStepEntered(step);
            else Debug.LogWarning($"[Hegra] No stop handles step {step}.");
        }

        string DescribeControls()
        {
            return HegraInput.Describe(Input.menu);
        }

        public void SaveNow()
        {
            if (State != null) Saves.SaveProgress(State);
        }

        // ───────────────────────── Hints ─────────────────────────

        public void RequestHint()
        {
            if (State == null || !InGame) return;
            State.hintLevel = Mathf.Min(3, State.hintLevel + 1);
            ShowHint(State.hintLevel);
            if (State.hintLevel >= 3) ObjectiveArrowPulse();
        }

        void ShowHint(int level)
        {
            for (int l = level; l >= 1; l--)
            {
                var key = $"hint.{State.step}.{l}";
                if (Loc.Has(key)) { Objective.ShowHint(key); return; }
            }
            Objective.ShowHint("hint.generic");
        }

        void ObjectiveArrowPulse()
        {
            if (ObjectiveBeacon.Current) m_Menus?.PointTo(ObjectiveBeacon.Current);
        }

        // ───────────────────────── Assisted route ─────────────────────────

        void OfferAssist()
        {
            if (AssistOfferOpen || m_AssistRunning) return;
            AssistOfferOpen = true;
            if (KamkamGuide.Instance) KamkamGuide.Instance.AppearNearPlayer();
            m_Dialogue.Play("assist_offer");
            m_Menus.ShowAssistOffer(AcceptAssist, DeclineAssist);
        }

        public void AcceptAssist()
        {
            AssistOfferOpen = false;
            m_Menus.CloseAssistOffer();
            if (State == null || State.step >= QuestStep.FaridInsertShard && State.assistedRoute) return;
            StartCoroutine(AssistRoutine());
        }

        public void DeclineAssist()
        {
            AssistOfferOpen = false;
            m_Menus.CloseAssistOffer();
            if (State != null) { State.assistOffersDeclined++; SaveNow(); }
            m_Dialogue.Play("assist_declined");
        }

        /// <summary>Also available from the pause menu at any time (accessibility).</summary>
        public void StartAssistedRouteFromMenu()
        {
            m_Menus.CloseAll();
            MenuOpen = false;
            AcceptAssist();
        }

        IEnumerator AssistRoutine()
        {
            m_AssistRunning = true;
            m_Dialogue.StopAll();
            if (m_ActiveStop) m_ActiveStop.OnStepExited(State.step);
            State.ApplyAssistedRoute();
            SaveNow();
            var farid = GetStop(StopId.Farid);
            yield return m_Rig.RelocateWithFade(farid && farid.spawnPoint ? farid.spawnPoint : m_Rig.transform, () =>
            {
                foreach (var s in m_Stops) if (s) s.RestoreState(State);
                RestoreInventory();
            });
            m_AssistRunning = false;
            EnterStep(State.step);
        }

        // ───────────────────────── Settings ─────────────────────────

        public void ApplySettings(SettingsData s, bool save = true)
        {
            s.Clamp();
            Settings = s;
            Loc.SetLanguage(s.language);
            if (m_Rig) m_Rig.ApplySettings(s);
            if (m_Body) m_Body.SetHandedness(s.handedness);
            if (m_LampCarry && m_LampCarry.IsCarrying) m_LampCarry.Carry(s.handedness);   // follow the new dominant hand
            if (m_Audio) m_Audio.ApplySettings(s);
            if (Subtitles) Subtitles.SetScale(s.subtitleScale);
            EduCard.SetVisible(s.educationalCards && (State != null ? State.museumMode : s.museumMode));
            SettingsChanged?.Invoke(s);
            if (save) Saves.SaveSettings(s);
        }

        // ───────────────────────── Per-frame ─────────────────────────

        void Update()
        {
            PollHeadset();
            // the carried lamp may be hung on the belt from the belt lesson onwards
            if (m_LampCarry) m_LampCarry.allowBeltHang = State != null && InGame && State.step >= QuestStep.CampStoreLamp;
            UpdatePresence();

            if (Input.menu.WasPressedThisFrame() && m_Menus) m_Menus.TogglePause();
            if (Input.advance.WasPressedThisFrame() && m_Dialogue) m_Dialogue.Advance();
            if (Input.hint.WasPressedThisFrame()) RequestHint();
            if (Input.inventory.WasPressedThisFrame() && m_Menus) m_Menus.ToggleInventory();

            if (State == null || !InGame) return;

            if (!IsTimerPaused)
            {
                State.elapsedSeconds += Time.unscaledDeltaTime;
                m_StepTimer += Time.unscaledDeltaTime;
                m_SaveTimer += Time.unscaledDeltaTime;
                if (m_SaveTimer > 20f) { m_SaveTimer = 0f; SaveNow(); }

                if (m_StepTimer > m_IdleHintSeconds && State.hintLevel == 0 && !m_Dialogue.IsPlaying)
                {
                    State.hintLevel = 1;
                    ShowHint(1);
                }

                if (!AssistOfferOpen && !m_AssistRunning && !m_Dialogue.IsPlaying &&
                    QuestFlow.ShouldOfferAssist(State, m_LateAssistSeconds, m_ReofferSeconds))
                    OfferAssist();
            }

            float night = NightProgress;
            if (m_Moon) m_Moon.SetProgress(night);
            if (m_Audio) m_Audio.SetNightProgress(night);
        }

        float StageProgress()
        {
            if (State == null) return 0f;
            int idx = 0, cur = 0;
            foreach (var s in QuestFlow.Steps)
            {
                if (s.step == State.step) cur = idx;
                idx++;
            }
            return idx > 1 ? (float)cur / (idx - 1) : 0f;
        }

        void PollHeadset()
        {
            m_PresenceTimer -= Time.unscaledDeltaTime;
            if (m_PresenceTimer > 0f) return;
            m_PresenceTimer = 0.5f;
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (head.isValid && head.TryGetFeatureValue(CommonUsages.userPresence, out bool present))
            {
                // trust "not present" only once the sensor has reported "present" at least once: a runtime that
                // always reports false must not hold the game paused forever
                if (present) m_SeenPresent = true;
                m_RawPresent = present || !m_SeenPresent;
            }
            else
                m_RawPresent = true; // simulator / no presence sensor
        }

        void UpdatePresence()
        {
            if (m_RawFocus && m_RawPresent)
            {
                m_AwaySince = -1f;
                AppFocused = true;
                HeadsetPresent = true;
                return;
            }
            if (m_AwaySince < 0f) m_AwaySince = Time.unscaledTime;
            if (Time.unscaledTime - m_AwaySince < PauseDebounceSeconds) return;
            bool wasActive = AppFocused && HeadsetPresent;
            AppFocused = m_RawFocus;
            HeadsetPresent = m_RawPresent;
            if (wasActive) SaveNow(); // once per real pause, not on every blip
        }

        void OnApplicationFocus(bool focus)
        {
#if UNITY_EDITOR
            // In the Editor, clicking another window (console, IDE) is not the player leaving the experience;
            // pausing there froze dialogue during desktop tests. On device, focus loss (system menu) still pauses.
            focus = true;
#endif
            m_RawFocus = focus;
        }

        void OnApplicationPause(bool paused)
        {
            // the app is suspended (no frames run): pause and save right away; resuming recovers on the next frame
            m_RawFocus = !paused;
            if (paused)
            {
                AppFocused = false;
                SaveNow();
            }
            else m_AwaySince = -1f;
        }

        void OnApplicationQuit() => SaveNow();
    }
}
