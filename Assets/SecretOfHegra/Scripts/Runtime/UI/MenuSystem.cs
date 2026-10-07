using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SecretOfHegra
{
    /// <summary>
    /// All world-space menus, built at runtime so they always match the current language and settings:
    /// main menu, pause, settings, inventory, toasts, the assisted-route offer and the ending screen.
    /// Panels open in front of the player and stay put (they never follow the head while being used).
    /// </summary>
    public class MenuSystem : MonoBehaviour
    {
        GameManager m_Game;
        Camera m_Cam;
        Transform m_Head;

        GameObject m_Main, m_Pause, m_Settings, m_Inventory, m_Assist, m_Ending, m_Confirm;
        GameObject m_ToastRoot;
        TextMeshProUGUI m_ToastText;
        float m_ToastUntil;
        GameObject m_Arrow;
        Transform m_ArrowTarget;
        float m_ArrowUntil;

        readonly List<Action> m_Refreshers = new List<Action>();
        GameObject m_SettingsReturnTo;

        public Action SaveSouvenirAction;
        public bool AnyOpen => IsOpen(m_Main) || IsOpen(m_Pause) || IsOpen(m_Settings) || IsOpen(m_Inventory) || IsOpen(m_Ending) || IsOpen(m_Confirm);

        static bool IsOpen(GameObject g) => g && g.activeSelf;

        public void Initialize(GameManager game)
        {
            m_Game = game;
            m_Cam = game.Rig.Camera;
            m_Head = game.Rig.Head;
            BuildToast();
            BuildArrow();
            Loc.LanguageChanged += RefreshAll;
        }

        void OnDestroy() => Loc.LanguageChanged -= RefreshAll;

        void RefreshAll()
        {
            foreach (var r in m_Refreshers) r?.Invoke();
        }

        // ───────────── helpers ─────────────

        float m_RowWidth = 600f;

        GameObject NewPanel(string name, Vector2 size, string titleKey, out RectTransform content)
        {
            var root = new GameObject(name);
            root.transform.SetParent(transform, false);
            var canvas = UIFactory.CreateCanvas(name + "Canvas", root.transform, size, m_Cam, 20);
            var rt = (RectTransform)canvas.transform;
            UIFactory.Panel(rt, HegraPalette.PanelBg, "Bg");
            content = UIFactory.Rect("Content", rt);
            UIFactory.Stretch(content, 20);
            m_RowWidth = size.x - 40f;
            UIFactory.VerticalLayout(content, 12, 0);
            if (!string.IsNullOrEmpty(titleKey))
            {
                var title = UIFactory.LayoutText(content, "Title", size.x - 40, 70, 44, TextAlignmentOptions.Center);
                title.color = HegraPalette.Lantern;
                title.gameObject.AddComponent<LocalizedText>().SetKey(titleKey, TextAlignmentOptions.Center);
            }
            root.SetActive(false);
            return root;
        }

        void PlaceInFront(GameObject panel, float distance = 1.25f, float height = -0.08f)
        {
            var fwd = Vector3.ProjectOnPlane(m_Head.forward, Vector3.up).normalized;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            panel.transform.SetPositionAndRotation(m_Head.position + fwd * distance + Vector3.up * height, Quaternion.LookRotation(fwd));
        }

        GameObject m_Settling;
        float m_SettleUntil;

        void Open(GameObject panel)
        {
            PlaceInFront(panel);
            // keep re-centering briefly: the head pose may still be settling (startup, recenter, relocation)
            m_Settling = panel;
            m_SettleUntil = Time.unscaledTime + 1.5f;
            panel.SetActive(true);
            m_Game.MenuOpen = AnyOpen && !IsOpen(m_Ending);
            RefreshAll();
        }

        void Close(GameObject panel)
        {
            if (panel) panel.SetActive(false);
            m_Game.MenuOpen = AnyOpen && !IsOpen(m_Ending);
        }

        public void CloseAll()
        {
            Close(m_Main); Close(m_Pause); Close(m_Settings); Close(m_Inventory); Close(m_Confirm); Close(m_Ending);
            m_Game.MenuOpen = false;
        }

        Button AddButton(RectTransform parent, string key, Action a, float width = 520f) =>
            UIFactory.Button(parent, key, key, a, width, 70, 30);

        TextMeshProUGUI AddValueRow(RectTransform parent, string labelKey, Func<string> value, Action dec, Action inc, Action toggle = null)
        {
            var row = UIFactory.Row(parent, labelKey, 64, 10, m_RowWidth);
            float half = m_RowWidth * 0.5f - 8f;
            var label = UIFactory.LayoutText(row, "Label", half, 60, 24, TextAlignmentOptions.Center);
            label.gameObject.AddComponent<LocalizedText>().SetKey(labelKey, TextAlignmentOptions.Center);
            TextMeshProUGUI val;
            if (toggle != null)
            {
                var b = UIFactory.Button(row, "Toggle", "ui.empty", () => { toggle(); RefreshAll(); }, half, 60, 24);
                Destroy(b.GetComponentInChildren<LocalizedText>());
                val = b.GetComponentInChildren<TextMeshProUGUI>();
            }
            else
            {
                UIFactory.Button(row, "Dec", "ui.minus", () => { dec(); RefreshAll(); }, 70, 60, 34);
                val = UIFactory.LayoutText(row, "Value", half - 160f, 60, 24, TextAlignmentOptions.Center);
                UIFactory.Button(row, "Inc", "ui.plus", () => { inc(); RefreshAll(); }, 70, 60, 34);
            }
            var v = val;
            m_Refreshers.Add(() => { if (v) Loc.Apply(v, value(), TextAlignmentOptions.Center); });
            return val;
        }

        // ───────────── main menu ─────────────

        public void ShowMain()
        {
            if (!m_Main) BuildMain();
            CloseAll();
            Open(m_Main);
        }

        Button m_ContinueButton;

        void BuildMain()
        {
            m_Main = NewPanel("MainMenu", new Vector2(640, 470), "game.title", out var c);
            var sub = UIFactory.LayoutText(c, "Subtitle", 600, 50, 24, TextAlignmentOptions.Center);
            sub.gameObject.AddComponent<LocalizedText>().SetKey("game.subtitle", TextAlignmentOptions.Center);
            AddButton(c, "menu.new", () => m_Game.RequestNewGame());
            m_ContinueButton = AddButton(c, "menu.continue", () => m_Game.RequestContinue());
            // the main menu is intentionally minimal: title, subtitle, new journey, continue, exit. Language, museum
            // mode and the other settings keep their saved/default values (Arabic) and stay reachable in-game from
            // the pause menu's settings.
            AddButton(c, "menu.quit", Application.Quit);
            m_Refreshers.Add(() => { if (m_ContinueButton) m_ContinueButton.interactable = m_Game.Saves.HasProgress(); });
        }

        // ───────────── pause ─────────────

        public void TogglePause()
        {
            if (!m_Game.InGame) return;
            if (IsOpen(m_Ending)) return;
            if (AnyOpen) { CloseAll(); return; }
            if (!m_Pause) BuildPause();
            Open(m_Pause);
        }

        Button m_ResetPuzzleButton;
        Button m_AssistButton;

        void BuildPause()
        {
            m_Pause = NewPanel("PauseMenu", new Vector2(640, 940), "menu.paused", out var c);
            AddButton(c, "menu.resume", CloseAll);
            AddButton(c, "menu.hint", () => { CloseAll(); m_Game.RequestHint(); });
            m_ResetPuzzleButton = AddButton(c, "menu.reset_puzzle", () =>
            {
                CloseAll();
                var stop = m_Game.GetStop(QuestFlow.StopOf(m_Game.State.step));
                if (stop && stop.CanResetPuzzle) stop.ResetPuzzle();
            });
            m_AssistButton = AddButton(c, "menu.assist", () => m_Game.StartAssistedRouteFromMenu());
            AddButton(c, "menu.inventory", () => { Close(m_Pause); ToggleInventory(); });
            AddButton(c, "menu.settings", () => OpenSettings(m_Pause));
            AddButton(c, "menu.restart", () => Confirm("menu.restart_confirm", m_Game.RequestRestart));
            AddButton(c, "menu.main", () => m_Game.RequestReturnToMenu());
            m_Refreshers.Add(() =>
            {
                if (m_Game.State == null) return;
                var stop = m_Game.GetStop(QuestFlow.StopOf(m_Game.State.step));
                if (m_ResetPuzzleButton) m_ResetPuzzleButton.interactable = stop && stop.CanResetPuzzle;
                if (m_AssistButton) m_AssistButton.interactable = !m_Game.State.assistedRoute && m_Game.State.step < QuestStep.FaridInsertShard;
            });
        }

        void Confirm(string key, Action yes)
        {
            if (m_Confirm) Destroy(m_Confirm);
            m_Confirm = NewPanel("Confirm", new Vector2(600, 360), key, out var c);
            var row = UIFactory.Row(c, "Row", 80, 20, m_RowWidth);
            UIFactory.Button(row, "Yes", "ui.yes", () => { Close(m_Confirm); yes(); }, 240, 70, 30);
            UIFactory.Button(row, "No", "ui.no", () => Close(m_Confirm), 240, 70, 30);
            Open(m_Confirm);
            m_Confirm.transform.position -= m_Confirm.transform.forward * 0.05f;
        }

        // ───────────── settings ─────────────

        void OpenSettings(GameObject returnTo)
        {
            if (!m_Settings) BuildSettings();
            m_SettingsReturnTo = returnTo;
            Close(returnTo);
            Open(m_Settings);
        }

        void BuildSettings()
        {
            m_Settings = NewPanel("Settings", new Vector2(820, 1400), "menu.settings", out var c);
            SettingsData S() => m_Game.Settings;
            void Apply() => m_Game.ApplySettings(S());
            string OnOff(bool b) => Loc.Get(b ? "ui.on" : "ui.off");
            string Pct(float v) => Mathf.RoundToInt(v * 100f) + "%";

            AddValueRow(c, "set.language", () => Loc.Get("lang.name"), null, null, () =>
            { S().language = S().language == Language.Arabic ? Language.English : Language.Arabic; Apply(); });
            AddValueRow(c, "set.subtitles", () => Pct(S().subtitleScale), () => { S().subtitleScale -= 0.1f; Apply(); }, () => { S().subtitleScale += 0.1f; Apply(); });
            AddValueRow(c, "set.posture", () => Loc.Get(S().seatedMode ? "set.seated" : "set.standing"), null, null, () => { S().seatedMode = !S().seatedMode; Apply(); });
            AddValueRow(c, "set.height", () => (S().interactionHeightOffset >= 0 ? "+" : "") + Mathf.RoundToInt(S().interactionHeightOffset * 100f) + " cm",
                () => { S().interactionHeightOffset -= 0.05f; Apply(); }, () => { S().interactionHeightOffset += 0.05f; Apply(); });
            AddValueRow(c, "set.hand", () => Loc.Get(S().handedness == Handedness.Right ? "set.right" : "set.left"), null, null,
                () => { S().handedness = S().handedness == Handedness.Right ? Handedness.Left : Handedness.Right; Apply(); });
            AddValueRow(c, "set.movement", () => Loc.Get(S().smoothMovement ? "set.smooth" : "set.teleport"), null, null, () => { S().smoothMovement = !S().smoothMovement; Apply(); });
            AddValueRow(c, "set.speed", () => Loc.Get(S().moveSpeed == MoveSpeed.Slow ? "set.speed_slow" : S().moveSpeed == MoveSpeed.Fast ? "set.speed_fast" : "set.speed_normal"),
                () => { S().moveSpeed = (MoveSpeed)Mathf.Max(0, (int)S().moveSpeed - 1); Apply(); },
                () => { S().moveSpeed = (MoveSpeed)Mathf.Min(2, (int)S().moveSpeed + 1); Apply(); });
            AddValueRow(c, "set.turn", () => Loc.Get(S().smoothTurn ? "set.turn_smooth" : "set.turn_snap"), null, null, () => { S().smoothTurn = !S().smoothTurn; Apply(); });
            AddValueRow(c, "set.vignette", () => Pct(S().vignetteStrength), () => { S().vignetteStrength -= 0.25f; Apply(); }, () => { S().vignetteStrength += 0.25f; Apply(); });
            AddValueRow(c, "set.snap", () => Mathf.RoundToInt(S().snapTurnAngle) + "°", () => { S().snapTurnAngle = StepAngle(S().snapTurnAngle, -1); Apply(); }, () => { S().snapTurnAngle = StepAngle(S().snapTurnAngle, 1); Apply(); });
            AddValueRow(c, "set.appearance", () => Loc.Get(S().appearance == Appearance.Female ? "set.female" : "set.male"), null, null,
                () => { S().appearance = S().appearance == Appearance.Female ? Appearance.Male : Appearance.Female; Apply(); });
            AddValueRow(c, "set.museum", () => OnOff(S().museumMode), null, null, () => { S().museumMode = !S().museumMode; Apply(); });
            AddValueRow(c, "set.cards", () => OnOff(S().educationalCards), null, null, () => { S().educationalCards = !S().educationalCards; Apply(); });
            AddValueRow(c, "set.music", () => Pct(S().musicVolume), () => { S().musicVolume -= 0.1f; Apply(); }, () => { S().musicVolume += 0.1f; Apply(); });
            AddValueRow(c, "set.ambience", () => Pct(S().ambienceVolume), () => { S().ambienceVolume -= 0.1f; Apply(); }, () => { S().ambienceVolume += 0.1f; Apply(); });
            AddValueRow(c, "set.sfx", () => Pct(S().sfxVolume), () => { S().sfxVolume -= 0.1f; Apply(); }, () => { S().sfxVolume += 0.1f; Apply(); });
            AddValueRow(c, "set.dialogue", () => Pct(S().dialogueVolume), () => { S().dialogueVolume -= 0.1f; Apply(); }, () => { S().dialogueVolume += 0.1f; Apply(); });
            AddButton(c, "ui.back", () =>
            {
                Close(m_Settings);
                if (m_SettingsReturnTo) Open(m_SettingsReturnTo);
            });
        }

        static float StepAngle(float current, int dir)
        {
            float[] steps = { 30f, 45f, 60f, 90f };
            int i = Array.IndexOf(steps, current);
            if (i < 0) i = 1;
            return steps[Mathf.Clamp(i + dir, 0, steps.Length - 1)];
        }

        // ───────────── inventory ─────────────

        public void ToggleInventory()
        {
            if (!m_Game.InGame || IsOpen(m_Ending)) return;
            if (IsOpen(m_Inventory)) { Close(m_Inventory); return; }
            if (!m_Inventory) BuildInventory();
            Open(m_Inventory);
        }

        void BuildInventory()
        {
            m_Inventory = NewPanel("Inventory", new Vector2(760, 880), "inv.title", out var c);
            GameState St() => m_Game.State;
            string Have(bool b) => Loc.Get(b ? "inv.have" : "inv.missing");

            // the lamp is the player's from the first second of a journey (in hand, then on the belt)
            AddValueRow(c, "inv.lamp", () => Have(St() != null), null, null, () =>
            {
                if (m_Game.Lamp && St() != null) m_Game.Lamp.Recover();
            });
            AddValueRow(c, "inv.coins", () => St() == null ? "0" : St().coins.ToString(), null, null, () => { });
            AddValueRow(c, "inv.kohl", () => Loc.Get(St() != null && St().kohlApplied ? "inv.applied" : (St() != null && St().hasKohl ? "inv.have" : "inv.missing")), null, null, () => { });
            AddValueRow(c, "inv.shard", () => St() != null && St().shardEngraved ? St().playerName : Loc.Get("inv.missing"), null, null, () =>
            {
                var shard = NameShard.Instance;
                if (shard && St() != null && St().shardEngraved && !St().shardInserted) shard.Item.Recover();
            });
            AddValueRow(c, "inv.sign1", () => Have(St() != null && St().signInscription), null, null, () => { });
            AddValueRow(c, "inv.sign2", () => Have(St() != null && St().signWater), null, null, () => { });
            AddValueRow(c, "inv.sign3", () => Have(St() != null && St().signEagle), null, null, () => { });
            var help = UIFactory.LayoutText(c, "Help", 700, 80, 20, TextAlignmentOptions.Center);
            help.color = HegraPalette.Moonlight;
            help.gameObject.AddComponent<LocalizedText>().SetKey("inv.help", TextAlignmentOptions.Center);
            AddButton(c, "ui.close", () => Close(m_Inventory));
        }

        // ───────────── toast / arrow ─────────────

        void BuildToast()
        {
            m_ToastRoot = new GameObject("Toast");
            m_ToastRoot.transform.SetParent(transform, false);
            var follow = m_ToastRoot.AddComponent<LazyFollow>();
            follow.target = m_Head;
            follow.distance = 1.3f;
            follow.heightOffset = 0.18f;
            follow.yawThreshold = 20f;
            var canvas = UIFactory.CreateCanvas("ToastCanvas", m_ToastRoot.transform, new Vector2(760, 110), m_Cam, 30);
            var rt = (RectTransform)canvas.transform;
            var group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            UIFactory.Panel(rt, new Color(0.25f, 0.15f, 0.08f, 0.85f), "Bg");
            m_ToastText = UIFactory.Text(rt, "Text", 32, TextAlignmentOptions.Center, HegraPalette.Lantern);
            UIFactory.Stretch((RectTransform)m_ToastText.transform, 10);
            m_ToastRoot.SetActive(false);
        }

        public void ShowToast(string key, float seconds = 3.5f)
        {
            ShowToastRaw(Loc.Get(key), seconds);
        }

        public void ShowToastRaw(string text, float seconds = 3.5f)
        {
            if (!m_ToastRoot) return;
            Loc.Apply(m_ToastText, text, TextAlignmentOptions.Center);
            m_ToastRoot.SetActive(true);
            m_ToastRoot.GetComponent<LazyFollow>().SnapNow();
            m_ToastUntil = Time.unscaledTime + seconds;
        }

        void BuildArrow()
        {
            m_Arrow = new GameObject("GuideArrow");
            m_Arrow.transform.SetParent(transform, false);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = HegraPalette.Lantern };
            void Part(Vector3 pos, Vector3 scale, Vector3 euler)
            {
                var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(p.GetComponent<Collider>());
                p.transform.SetParent(m_Arrow.transform, false);
                p.transform.localPosition = pos;
                p.transform.localScale = scale;
                p.transform.localEulerAngles = euler;
                p.GetComponent<Renderer>().sharedMaterial = mat;
            }
            Part(new Vector3(0, 0, -0.03f), new Vector3(0.02f, 0.02f, 0.12f), Vector3.zero);
            Part(new Vector3(0.025f, 0, 0.03f), new Vector3(0.015f, 0.02f, 0.07f), new Vector3(0, -40, 0));
            Part(new Vector3(-0.025f, 0, 0.03f), new Vector3(0.015f, 0.02f, 0.07f), new Vector3(0, 40, 0));
            m_Arrow.SetActive(false);
        }

        /// <summary>Shows a small floating arrow in front of the player pointing toward the target for a few seconds.</summary>
        public void PointTo(Transform target, float seconds = 6f)
        {
            m_ArrowTarget = target;
            m_ArrowUntil = Time.unscaledTime + seconds;
            m_Arrow.SetActive(true);
        }

        void LateUpdate()
        {
            if (m_Settling && m_Settling.activeSelf && Time.unscaledTime < m_SettleUntil) PlaceInFront(m_Settling);
            if (m_ToastRoot && m_ToastRoot.activeSelf && Time.unscaledTime > m_ToastUntil) m_ToastRoot.SetActive(false);
            if (m_Arrow && m_Arrow.activeSelf)
            {
                if (!m_ArrowTarget || Time.unscaledTime > m_ArrowUntil) { m_Arrow.SetActive(false); return; }
                var fwd = Vector3.ProjectOnPlane(m_Head.forward, Vector3.up).normalized;
                var pos = m_Head.position + fwd * 0.7f - Vector3.up * 0.25f;
                var dir = m_ArrowTarget.position - pos;
                dir.y = 0;
                m_Arrow.transform.SetPositionAndRotation(pos, dir.sqrMagnitude > 0.01f ? Quaternion.LookRotation(dir) : Quaternion.identity);
            }
        }

        // ───────────── assisted route offer ─────────────

        public void ShowAssistOffer(Action accept, Action decline)
        {
            if (m_Assist) Destroy(m_Assist);
            m_Assist = NewPanel("AssistOffer", new Vector2(700, 420), "assist.title", out var c);
            var body = UIFactory.LayoutText(c, "Body", 660, 120, 26, TextAlignmentOptions.Center);
            body.gameObject.AddComponent<LocalizedText>().SetKey("assist.body", TextAlignmentOptions.Center);
            var row = UIFactory.Row(c, "Row", 80, 20, m_RowWidth);
            UIFactory.Button(row, "Accept", "assist.accept", accept, 300, 70, 28);
            UIFactory.Button(row, "Decline", "assist.decline", decline, 300, 70, 28);
            PlaceInFront(m_Assist, 1.15f, -0.2f);
            m_Assist.SetActive(true);
        }

        public void CloseAssistOffer()
        {
            if (m_Assist) Destroy(m_Assist);
        }

        // ───────────── ending ─────────────

        public void ShowEnding()
        {
            if (m_Ending) Destroy(m_Ending);
            m_Ending = NewPanel("Ending", new Vector2(700, 640), "end.title", out var c);
            var body = UIFactory.LayoutText(c, "Body", 660, 150, 26, TextAlignmentOptions.Center);
            body.gameObject.AddComponent<LocalizedText>().SetKey("end.body", TextAlignmentOptions.Center);
            if (SaveSouvenirAction != null)
                AddButton(c, "end.save_souvenir", () => SaveSouvenirAction());
            AddButton(c, "end.replay", () => m_Game.RequestRestart());
            AddButton(c, "menu.main", () => m_Game.RequestReturnToMenu());
            PlaceInFront(m_Ending, 1.3f, -0.15f);
            m_Ending.SetActive(true);
        }
    }
}
