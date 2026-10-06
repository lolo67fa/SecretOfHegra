using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;

namespace SecretOfHegra
{
    /// <summary>
    /// Stop 5 — Qasr al-Bint. From the watching stone the player looks at the eagle carved above the
    /// doorway; it wakes and casts a thread of light toward one star. Looking at that star (head direction,
    /// held until the ring fills) grants Sign 3. Pointing the ray and pulling the trigger works too, from
    /// anywhere, so the puzzle never depends on neck movement alone.
    /// </summary>
    public class BintStop : StopBase
    {
        public Transform facadeCenter;
        public Transform watchingStone;
        public float stoneRadius = 1.6f;

        [Header("Eagle")]
        public Transform eagle;
        public Transform eagleBeak;
        public Renderer eagleGlow;
        public Transform eagleWingL, eagleWingR;
        public XRSimpleInteractable eagleInteractable;

        [Header("Stars")]
        public List<Transform> stars = new List<Transform>();
        public int correctStar;
        public List<XRSimpleInteractable> starInteractables = new List<XRSimpleInteractable>();
        public Transform lightThread;
        public Renderer correctStarHalo;

        [Header("Feedback")]
        public GazeRing ring;
        public Transform toFarid;
        public float arriveDistance = 11f;
        public float gazeSeconds = 1.6f;
        public float eagleGazeAngle = 7f;
        public float starGazeAngle = 5f;

        /// <summary>Automation only: overrides the head's forward direction for the gaze test.</summary>
        [System.NonSerialized] public Vector3? debugGazeDirection;

        bool m_IntroStarted, m_EagleAwake, m_Solved, m_WrongCooldown, m_StoneToastShown;
        Transform m_GazeTarget;
        float m_Gaze;
        Quaternion m_WingLRest, m_WingRRest;

        public bool EagleAwake => m_EagleAwake;
        public bool Solved => m_Solved;

        public override void Initialize()
        {
            if (eagleWingL) m_WingLRest = eagleWingL.localRotation;
            if (eagleWingR) m_WingRRest = eagleWingR.localRotation;
            if (eagleInteractable) eagleInteractable.selectEntered.AddListener(_ => OnTargetChosen(eagle));
            for (int i = 0; i < starInteractables.Count; i++)
            {
                var star = i < stars.Count ? stars[i] : null;
                if (starInteractables[i]) starInteractables[i].selectEntered.AddListener(_ => OnTargetChosen(star));
            }
            SetEagle(false, true);
            if (ring) ring.Hide();
        }

        public override void RestoreState(GameState s)
        {
            m_Solved = s.signEagle;
            SetEagle(s.signEagle, true);
            SetStarSolved(s.signEagle);
        }

        public override void OnStepEntered(QuestStep step)
        {
            switch (step)
            {
                case QuestStep.BintIntro:
                    ObjectiveBeacon.Current = watchingStone;
                    m_IntroStarted = false;
                    break;
                case QuestStep.BintAlign:
                    ObjectiveBeacon.Current = m_EagleAwake ? stars[correctStar] : watchingStone;
                    Game.Objective.SetActions(("menu.reset_puzzle", ResetPuzzle));
                    SetFarReach(true);
                    break;
            }
        }

        public override void OnStepExited(QuestStep step)
        {
            if (ring) ring.Hide();
            SetFarReach(false);
        }

        // The stars are ~40 m away, far beyond the default 10 m far ray; extend it only during this puzzle.
        readonly Dictionary<CurveInteractionCaster, float> m_DefaultReach = new Dictionary<CurveInteractionCaster, float>();

        void SetFarReach(bool extended)
        {
            foreach (var nf in Game.Rig.GetComponentsInChildren<NearFarInteractor>(true))
            {
                if (!(nf.farInteractionCaster is CurveInteractionCaster c)) continue;
                if (!m_DefaultReach.ContainsKey(c)) m_DefaultReach[c] = c.castDistance;
                c.castDistance = extended ? 60f : m_DefaultReach[c];
            }
        }

        void Update()
        {
            if (Game == null || Game.State == null || !Game.InGame) return;
            var head = Game.Rig.Head;
            if (IsCurrent(QuestStep.BintIntro) && !m_IntroStarted && facadeCenter)
            {
                var d = head.position - facadeCenter.position; d.y = 0;
                if (d.magnitude < arriveDistance)
                {
                    m_IntroStarted = true;
                    Game.Dialogue.Play("bint_intro", () => Complete(QuestStep.BintIntro));
                }
            }

            if (correctStarHalo)
                correctStarHalo.enabled = m_Solved || (m_EagleAwake && IsCurrent(QuestStep.BintAlign) && Game.State.hintLevel >= 3);

            if (!IsCurrent(QuestStep.BintAlign) || m_Solved || Game.MenuOpen) { if (ring) ring.Hide(); return; }
            UpdateGaze(head);
        }

        bool OnStone(Vector3 headPos)
        {
            if (!watchingStone) return true;
            var d = headPos - watchingStone.position; d.y = 0;
            return d.magnitude <= stoneRadius;
        }

        void UpdateGaze(Transform head)
        {
            Transform looked = null;
            if (OnStone(head.position))
            {
                var fwd = debugGazeDirection ?? head.forward;
                if (!m_EagleAwake) looked = Looking(head.position, fwd, eagle, eagleGazeAngle);
                else
                    foreach (var s in stars)
                        if (Looking(head.position, fwd, s, starGazeAngle)) { looked = s; break; }
            }
            else if (!m_StoneToastShown && Looking(head.position, debugGazeDirection ?? head.forward, eagle, eagleGazeAngle * 2f))
            {
                m_StoneToastShown = true; // looking at the eagle from the wrong spot: point to the stone once
                Game.Menus.ShowToast("toast.bint_stand_on_stone");
            }

            if (looked != m_GazeTarget) { m_GazeTarget = looked; m_Gaze = 0f; m_WrongCooldown = false; }
            if (!looked) { if (ring) ring.Hide(); return; }

            m_Gaze += Time.deltaTime;
            if (ring)
            {
                ring.ShowAt(looked.position, head.position, looked == eagle ? 1f : 2.6f);
                ring.SetProgress(m_Gaze / gazeSeconds);
            }
            if (m_Gaze >= gazeSeconds && !m_WrongCooldown) OnTargetChosen(looked);
        }

        static Transform Looking(Vector3 from, Vector3 fwd, Transform target, float angle)
        {
            if (!target) return null;
            return Vector3.Angle(fwd, target.position - from) <= angle ? target : null;
        }

        void OnTargetChosen(Transform target)
        {
            if (!target || m_Solved || !IsCurrent(QuestStep.BintAlign)) return;
            if (target == eagle)
            {
                if (m_EagleAwake) return;
                m_EagleAwake = true;
                m_GazeTarget = null; m_Gaze = 0f;
                StartCoroutine(WakeEagle());
                return;
            }
            if (!m_EagleAwake) return; // the stars mean nothing until the eagle shows the way
            if (stars.IndexOf(target) == correctStar) Solve();
            else
            {
                m_WrongCooldown = true;
                HegraAudio.PlayAt(Sfx.Fail, target.position, 0.4f);
                Game.Menus.ShowToast("toast.bint_wrong_star");
            }
        }

        IEnumerator WakeEagle()
        {
            HegraAudio.PlayAt(Sfx.Carve, eagle.position, 0.9f);
            if (ring) ring.Hide();
            for (float t = 0; t < 1.2f; t += Time.deltaTime)
            {
                SetWings(Mathf.Sin(t / 1.2f * Mathf.PI) * 28f + t / 1.2f * 12f);
                if (eagleGlow) eagleGlow.enabled = true;
                yield return null;
            }
            SetEagle(true, false);
            Game.Dialogue.Play("bint_eagle");
            ObjectiveBeacon.Current = stars[correctStar];
        }

        void Solve()
        {
            m_Solved = true;
            if (ring) ring.Hide();
            SetStarSolved(true);
            Game.Dialogue.Play("bint_done");
            SetFarReach(false);
            Game.GrantSign(SignId.Eagle);
            ObjectiveBeacon.Current = toFarid;
        }

        void SetWings(float lift)
        {
            if (eagleWingL) eagleWingL.localRotation = m_WingLRest * Quaternion.Euler(0, 0, lift);
            if (eagleWingR) eagleWingR.localRotation = m_WingRRest * Quaternion.Euler(0, 0, -lift);
        }

        void SetEagle(bool awake, bool instant)
        {
            m_EagleAwake = awake;
            if (eagleGlow) eagleGlow.enabled = awake;
            if (lightThread) lightThread.gameObject.SetActive(awake);
            SetWings(awake ? 12f : 0f);
        }

        void SetStarSolved(bool solved)
        {
            if (correctStarHalo) correctStarHalo.enabled = solved;
            if (solved && correctStar < stars.Count && stars[correctStar])
                stars[correctStar].localScale = Vector3.one * 1.8f;
        }

        public override bool CanResetPuzzle => IsCurrent(QuestStep.BintAlign) && !m_Solved;

        public override void ResetPuzzle()
        {
            if (m_Solved) return;
            StopAllCoroutines();
            SetEagle(false, true);
            m_GazeTarget = null; m_Gaze = 0f;
            if (ring) ring.Hide();
            ObjectiveBeacon.Current = watchingStone;
            Game.Menus.ShowToast("toast.puzzle_reset");
        }
    }
}
