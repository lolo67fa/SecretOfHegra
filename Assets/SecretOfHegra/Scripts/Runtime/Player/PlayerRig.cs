using System;
using System.Collections;
using System.Reflection;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

namespace SecretOfHegra
{
    /// <summary>
    /// Wraps the Starter Assets XR Origin: applies comfort/handedness/seated settings, reports teleport
    /// and snap-turn events for the tutorial, and performs faded relocations (never animates the camera).
    /// </summary>
    public class PlayerRig : MonoBehaviour
    {
        [SerializeField] XROrigin m_Origin;
        [SerializeField] Camera m_Camera;
        [SerializeField] Transform m_LeftController;
        [SerializeField] Transform m_RightController;
        [SerializeField] SnapTurnProvider m_SnapTurn;
        [SerializeField] ContinuousTurnProvider m_ContinuousTurn;
        [SerializeField] ContinuousMoveProvider m_Move;
        [SerializeField] TeleportationProvider m_Teleport;
        [SerializeField] TunnelingVignetteController m_Vignette;
        [SerializeField] ScreenFader m_Fader;

        public XROrigin Origin => m_Origin;
        public Camera Camera => m_Camera;
        public Transform Head => m_Camera ? m_Camera.transform : transform;
        public Transform LeftController => m_LeftController;
        public Transform RightController => m_RightController;
        public ScreenFader Fader => m_Fader;
        public Handedness Handedness { get; private set; } = Handedness.Right;
        public Transform DominantController => Handedness == Handedness.Right ? m_RightController : m_LeftController;
        public Transform OffHandController => Handedness == Handedness.Right ? m_LeftController : m_RightController;

        public event Action Teleported;
        public event Action SnapTurned;
        public event Action Relocated;

        public bool IsRelocating { get; private set; }

        float m_CameraOffsetY;

        public void Configure(XROrigin origin, Camera cam, Transform left, Transform right, SnapTurnProvider snap,
            ContinuousTurnProvider contTurn, ContinuousMoveProvider move, TeleportationProvider tp,
            TunnelingVignetteController vignette, ScreenFader fader)
        {
            m_Origin = origin; m_Camera = cam; m_LeftController = left; m_RightController = right;
            m_SnapTurn = snap; m_ContinuousTurn = contTurn; m_Move = move; m_Teleport = tp;
            m_Vignette = vignette; m_Fader = fader;
        }

        bool m_SimulatorChecked;

        /// <summary>
        /// The desktop XR Interaction Simulator reports the head at floor level. When a simulated HMD is
        /// present (editor only), use Device tracking with a standing eye height instead. Real headsets
        /// keep Floor tracking.
        /// </summary>
        void CheckSimulator()
        {
            if (m_SimulatorChecked || !m_Origin) return;
            foreach (var d in UnityEngine.InputSystem.InputSystem.devices)
            {
                if (d.name.Contains("XRSimulatedHMD"))
                {
                    m_SimulatorChecked = true;
                    m_Origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
                    m_Origin.CameraYOffset = 1.6f;
                    SpeedUpSimulator();
                    var hands = GetComponent<TravelerHands>();
                    if (hands) hands.SetSleevesVisible(false);
                    return;
                }
            }
        }

        void Awake()
        {
            // point + trigger selects puzzle targets (the Starter Assets put select on grip only)
            if (!GetComponent<TriggerSelectBridge>()) gameObject.AddComponent<TriggerSelectBridge>();
        }

        void OnEnable()
        {
            if (m_Teleport) m_Teleport.locomotionEnded += OnTeleportEnded;
            if (m_SnapTurn) m_SnapTurn.locomotionEnded += OnSnapTurnEnded;
        }

        void OnDisable()
        {
            if (m_Teleport) m_Teleport.locomotionEnded -= OnTeleportEnded;
            if (m_SnapTurn) m_SnapTurn.locomotionEnded -= OnSnapTurnEnded;
        }

        void OnTeleportEnded(LocomotionProvider _)
        {
            HegraAudio.PlayAt(Sfx.Footstep, Head.position - Vector3.up * 1.4f, 0.8f);
            Teleported?.Invoke();
        }

        void OnSnapTurnEnded(LocomotionProvider _) => SnapTurned?.Invoke();

        public void ApplySettings(SettingsData s)
        {
            Handedness = s.handedness;
            m_CameraOffsetY = s.CameraHeightOffset;
            ApplyCameraOffset();

            m_WalkSpeed = s.WalkSpeed;
            if (m_SnapTurn)
            {
                m_SnapTurn.turnAmount = s.snapTurnAngle;
                m_SnapTurn.debounceTime = 0.3f;          // snappier repeat than the 0.5 s default
                m_SnapTurn.enabled = true;
            }
            if (m_ContinuousTurn)
            {
                m_ContinuousTurn.turnSpeed = SettingsData.SmoothTurnSpeed;
                m_ContinuousTurn.enabled = true;
            }
            ApplyMoveSpeed();

            // Off hand walks smoothly when enabled; the dominant hand always keeps teleport + (snap or smooth) turn.
            bool rightHanded = s.handedness == Handedness.Right;
            SetControllerMode(m_LeftController, s.smoothMovement && rightHanded, s.smoothTurn && !rightHanded);
            SetControllerMode(m_RightController, s.smoothMovement && !rightHanded, s.smoothTurn && rightHanded);
            ConfigureTeleportReach();

            if (m_Vignette)
            {
                m_Vignette.enabled = s.vignetteStrength > 0.01f;
                var p = m_Vignette.defaultParameters;
                if (p != null)
                {
                    p.apertureSize = Mathf.Lerp(0.95f, 0.45f, s.vignetteStrength);
                    p.featheringEffect = 0.25f;
                }
            }
        }

        static void SetControllerMode(Transform controller, bool smoothMove, bool smoothTurn)
        {
            if (!controller) return;
            foreach (var mb in controller.GetComponents<MonoBehaviour>())
            {
                if (mb == null || mb.GetType().Name != "ControllerInputActionManager") continue;
                var prop = mb.GetType().GetProperty("smoothMotionEnabled", BindingFlags.Public | BindingFlags.Instance);
                prop?.SetValue(mb, smoothMove);
                var turn = mb.GetType().GetProperty("smoothTurnEnabled", BindingFlags.Public | BindingFlags.Instance);
                turn?.SetValue(mb, smoothTurn);
            }
        }

        // ───────── walking speed + sprint ─────────

        float m_WalkSpeed = 1.4f;
        bool m_Sprinting;
        float m_IdleTimer;

        public bool Sprinting => m_Sprinting;
        public float CurrentMoveSpeed => m_Move ? m_Move.moveSpeed : 0f;

        void ApplyMoveSpeed()
        {
            if (!m_Move) return;
            m_Move.moveSpeed = m_Sprinting ? Mathf.Min(4f, m_WalkSpeed * SettingsData.SprintMultiplier) : m_WalkSpeed;
        }

        public void SetSprint(bool on)
        {
            m_Sprinting = on;
            m_IdleTimer = 0f;
            ApplyMoveSpeed();
        }

        /// <summary>Click the walking stick to toggle sprint; it switches off when you stop moving.</summary>
        void UpdateSprint()
        {
            var gm = GameManager.Instance;
            if (!gm || gm.Input == null || !m_Move) return;
            if (gm.Input.sprint.WasPressedThisFrame()) SetSprint(!m_Sprinting);
            if (!m_Sprinting) return;
            var l = m_Move.leftHandMoveInput != null ? m_Move.leftHandMoveInput.ReadValue() : Vector2.zero;
            var r = m_Move.rightHandMoveInput != null ? m_Move.rightHandMoveInput.ReadValue() : Vector2.zero;
            m_IdleTimer = (l.sqrMagnitude + r.sqrMagnitude) < 0.01f ? m_IdleTimer + Time.deltaTime : 0f;
            if (m_IdleTimer > 0.6f) SetSprint(false);
        }

        /// <summary>Longer teleport arc (about 18 m on flat ground) so long hops between stops stay comfortable.</summary>
        void ConfigureTeleportReach()
        {
            foreach (var ray in GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>(true))
            {
                if (ray.name != "Teleport Interactor") continue;
                ray.velocity = 14f;
                ray.maxRaycastDistance = 40f;
            }
        }

        /// <summary>
        /// The XR Interaction Simulator moves the simulated headset with WASD at 0.2 m/s by default, which feels
        /// extremely slow on desktop. Raise it (editor/desktop testing only; no effect on Quest).
        /// </summary>
        static void SpeedUpSimulator()
        {
            var t = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRInteractionSimulator, Unity.XR.Interaction.Toolkit");
            if (t == null) return;
            foreach (var sim in FindObjectsByType(t, FindObjectsInactive.Exclude))
                foreach (var f in new[] { "m_TranslateXSpeed", "m_TranslateYSpeed", "m_TranslateZSpeed" })
                {
                    var field = t.GetField(f, BindingFlags.Instance | BindingFlags.NonPublic);
                    if (field != null && field.FieldType == typeof(float)) field.SetValue(sim, 1.4f);   // same as the Normal walking preset
                }
        }

        void Update()
        {
            UpdateSprint();
            UnstickSimulator();
        }

        // ───────── XR Interaction Simulator guard (desktop testing only) ─────────

        static System.Type s_SimType;
        static FieldInfo[] s_SimInputs, s_SimValues;
        UnityEngine.Object m_Simulator;

        /// <summary>
        /// The simulator caches WASD/QE translation per key press; if the release is missed (focus left the
        /// Game view while a key was held) the headset keeps gliding away. Clear a cached value whose key is up.
        /// </summary>
        void UnstickSimulator()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (!m_Simulator)
            {
                if (Time.frameCount % 60 != 0) return;
                s_SimType ??= System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRInteractionSimulator, Unity.XR.Interaction.Toolkit");
                if (s_SimType == null) return;
                m_Simulator = FindAnyObjectByType(s_SimType);
                if (!m_Simulator) return;
                const BindingFlags f = BindingFlags.Instance | BindingFlags.NonPublic;
                s_SimInputs = new[] { s_SimType.GetField("m_TranslateXInput", f), s_SimType.GetField("m_TranslateYInput", f), s_SimType.GetField("m_TranslateZInput", f) };
                s_SimValues = new[] { s_SimType.GetField("m_TranslateXValue", f), s_SimType.GetField("m_TranslateYValue", f), s_SimType.GetField("m_TranslateZValue", f) };
            }
            for (int i = 0; i < 3; i++)
            {
                var inF = s_SimInputs[i]; var valF = s_SimValues[i];
                if (inF == null || valF == null || valF.FieldType != typeof(float)) continue;
                var reader = inF.GetValue(m_Simulator);
                var read = reader?.GetType().GetMethod("ReadValue", System.Type.EmptyTypes);
                if (read == null || read.ReturnType != typeof(float)) continue;
                if (Mathf.Approximately((float)read.Invoke(reader, null), 0f) && !Mathf.Approximately((float)valF.GetValue(m_Simulator), 0f))
                    valF.SetValue(m_Simulator, 0f);
            }
#endif
        }

        void ApplyCameraOffset()
        {
            if (!m_Origin || !m_Origin.CameraFloorOffsetObject) return;
            var t = m_Origin.CameraFloorOffsetObject.transform;
            var p = t.localPosition;
            float baseY = m_Origin.RequestedTrackingOriginMode == XROrigin.TrackingOriginMode.Device ? m_Origin.CameraYOffset : 0f;
            float desired = baseY + m_CameraOffsetY;
            if (!Mathf.Approximately(p.y, desired))
            {
                p.y = desired;
                t.localPosition = p;
            }
        }

        void LateUpdate()
        {
            if (!m_SimulatorChecked && Application.isEditor && Time.frameCount % 30 == 0) CheckSimulator();
            // XR Origin may reset the floor offset when tracking origin changes; keep ours applied.
            ApplyCameraOffset();
        }

        /// <summary>Instantly places the player so the head is above <paramref name="target"/> facing its forward.</summary>
        public void PlaceAt(Transform target)
        {
            if (!m_Origin || !target) return;
            var fwd = Vector3.ProjectOnPlane(target.forward, Vector3.up).normalized;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            m_Origin.MatchOriginUpCameraForward(Vector3.up, fwd);
            var head = Head.position;
            var floor = m_Origin.Origin.transform.position;
            var desiredHead = target.position + Vector3.up * (head.y - floor.y);
            m_Origin.MoveCameraToWorldLocation(desiredHead);
            Relocated?.Invoke();
        }

        /// <summary>Comfortable relocation: fade out, move, fade in.</summary>
        public IEnumerator RelocateWithFade(Transform target, Action whileDark = null)
        {
            IsRelocating = true;
            if (m_Fader) yield return m_Fader.FadeTo(1f);
            PlaceAt(target);
            whileDark?.Invoke();
            yield return null;
            if (m_Fader) yield return m_Fader.FadeTo(0f);
            IsRelocating = false;
        }

        /// <summary>A point in front of the player at comfortable reach, for spawning recovered items.</summary>
        public Vector3 ReachPoint(float forward = 0.45f, float down = 0.35f)
        {
            var fwd = Vector3.ProjectOnPlane(Head.forward, Vector3.up).normalized;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            return Head.position + fwd * forward - Vector3.up * down;
        }

        public Vector3 FlatForward
        {
            get
            {
                var fwd = Vector3.ProjectOnPlane(Head.forward, Vector3.up).normalized;
                return fwd.sqrMagnitude < 0.01f ? Vector3.forward : fwd;
            }
        }
    }
}
