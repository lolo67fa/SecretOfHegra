using System;
using System.Collections;
using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// "The Mason and the Moon": a ~2 minute puppet play cued by Maliku's narration lines. Only stage props
    /// move; the player's camera is never touched. Skip jumps to the final tableau (moonbeam on the old
    /// wall); Replay restarts from the opening tableau.
    /// </summary>
    public class PerformanceDirector : MonoBehaviour
    {
        public NpcFigure maliku;
        public NpcFigure mason;
        public Transform masonStart, masonAtRock, masonExit;
        public Transform crown;            // carved crow-steps on the stage rock, grow in as he carves
        public Transform moon;
        public Transform moonLow, moonHigh;
        public Transform chest;
        public Transform tokenInscription, tokenWater, tokenEagle;
        public Transform moonbeam;         // stretched glow quad from the moon to the old wall
        public Transform beamTarget;

        public event Action Finished;
        public bool Playing { get; private set; }

        Vector3 m_CrownScale, m_TokenScale, m_ChestScale = Vector3.one;
        Coroutine m_Move;

        void Awake()
        {
            if (crown) m_CrownScale = crown.localScale;
            if (tokenInscription) m_TokenScale = tokenInscription.localScale;
            if (chest) m_ChestScale = chest.localScale;
            ResetTableau();
        }

        void OnEnable()
        {
            var gm = GameManager.Instance;
            if (gm && gm.Dialogue) gm.Dialogue.LineKeyStarted += OnLine;
        }

        void Start()
        {
            var gm = GameManager.Instance;
            if (gm && gm.Dialogue) { gm.Dialogue.LineKeyStarted -= OnLine; gm.Dialogue.LineKeyStarted += OnLine; }
        }

        void OnDisable()
        {
            var gm = GameManager.Instance;
            if (gm && gm.Dialogue) gm.Dialogue.LineKeyStarted -= OnLine;
        }

        public void ResetTableau()
        {
            Playing = false;
            if (mason) { mason.transform.position = masonStart.position; mason.transform.rotation = masonStart.rotation; mason.gameObject.SetActive(true); }
            if (crown) crown.localScale = new Vector3(m_CrownScale.x, 0.001f, m_CrownScale.z);
            if (moon) moon.position = moonLow.position;
            if (chest) chest.gameObject.SetActive(false);
            foreach (var t in new[] { tokenInscription, tokenWater, tokenEagle }) if (t) t.localScale = Vector3.zero;
            if (moonbeam) moonbeam.gameObject.SetActive(false);
        }

        public void Play(Action onFinished)
        {
            StopAllCoroutines();
            ResetTableau();
            Playing = true;
            if (maliku) maliku.idleGesture = NpcGesture.Strum;
            var gm = GameManager.Instance;
            gm.Audio?.SetMusicOverride(null);
            gm.Dialogue.Play("theater_play", () =>
            {
                FinalTableau();
                Playing = false;
                if (maliku) maliku.idleGesture = NpcGesture.Idle;
                onFinished?.Invoke();
                Finished?.Invoke();
            });
        }

        public void Skip() => GameManager.Instance.Dialogue.SkipSequence();

        /// <summary>The end state, also used when the performance was skipped or after loading a save.</summary>
        public void FinalTableau()
        {
            StopAllCoroutines();
            if (mason) mason.gameObject.SetActive(false);
            if (crown) crown.localScale = m_CrownScale;
            if (moon) moon.position = moonHigh.position;
            if (chest) chest.gameObject.SetActive(false);
            foreach (var t in new[] { tokenInscription, tokenWater, tokenEagle }) if (t) t.localScale = m_TokenScale;
            AimBeam();
        }

        void AimBeam()
        {
            if (!moonbeam || !moon || !beamTarget) return;
            moonbeam.gameObject.SetActive(true);
            var a = moon.position; var b = beamTarget.position;
            moonbeam.position = (a + b) * 0.5f;
            moonbeam.rotation = Quaternion.LookRotation(b - a) * Quaternion.Euler(90f, 0f, 0f);
            moonbeam.localScale = new Vector3(0.35f, (b - a).magnitude, 1f);
        }

        void OnLine(string key)
        {
            if (!Playing || string.IsNullOrEmpty(key) || !key.StartsWith("theater_play_")) return;
            switch (key)
            {
                case "theater_play_01":
                    Walk(masonAtRock, 5f);
                    break;
                case "theater_play_02":
                    mason.Play(NpcGesture.Carve, 9f);
                    StartCoroutine(Scale(crown, new Vector3(m_CrownScale.x, 0.001f, m_CrownScale.z), m_CrownScale, 8f));
                    break;
                case "theater_play_03":
                    StartCoroutine(MoveTo(moon, moonHigh.position, 6f));
                    mason.Play(NpcGesture.Wave, 4f);
                    break;
                case "theater_play_04":
                    mason.Play(NpcGesture.Talk, 8f);
                    if (chest) { chest.gameObject.SetActive(true); StartCoroutine(Scale(chest, Vector3.zero, m_ChestScale, 1.5f)); }
                    break;
                case "theater_play_05":
                    if (chest) chest.gameObject.SetActive(false);
                    StartCoroutine(Pulse(moon, 1.25f, 3f));
                    break;
                case "theater_play_06":
                    StartCoroutine(Scale(tokenInscription, Vector3.zero, m_TokenScale, 1.5f));
                    break;
                case "theater_play_07":
                    StartCoroutine(Scale(tokenWater, Vector3.zero, m_TokenScale, 1.5f));
                    break;
                case "theater_play_08":
                    StartCoroutine(Scale(tokenEagle, Vector3.zero, m_TokenScale, 1.5f));
                    break;
                case "theater_play_09":
                    mason.Play(NpcGesture.Idle, 1f);
                    Walk(masonExit, 6f, () => mason.gameObject.SetActive(false));
                    break;
                case "theater_play_10":
                    AimBeam();
                    if (maliku && beamTarget) maliku.Play(NpcGesture.Point, 8f, beamTarget);
                    break;
                case "theater_play_11":
                    if (maliku) maliku.Play(NpcGesture.Wave, 4f);
                    break;
            }
        }

        void Walk(Transform to, float seconds, Action done = null)
        {
            if (!mason || !to) return;
            if (m_Move != null) StopCoroutine(m_Move);
            m_Move = StartCoroutine(WalkRoutine(to, seconds, done));
        }

        IEnumerator WalkRoutine(Transform to, float seconds, Action done)
        {
            var t = mason.transform;
            var from = t.position;
            var dir = to.position - from; dir.y = 0;
            if (dir.sqrMagnitude > 0.001f) t.rotation = Quaternion.LookRotation(dir);
            mason.facePlayer = false;
            for (float e = 0; e < seconds; e += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, e / seconds);
                t.position = Vector3.Lerp(from, to.position, k) + Vector3.up * Mathf.Abs(Mathf.Sin(e * 6f)) * 0.03f;
                yield return null;
            }
            t.position = to.position;
            t.rotation = to.rotation;
            done?.Invoke();
        }

        static IEnumerator MoveTo(Transform t, Vector3 to, float seconds)
        {
            if (!t) yield break;
            var from = t.position;
            for (float e = 0; e < seconds; e += Time.deltaTime)
            {
                t.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0, 1, e / seconds));
                yield return null;
            }
            t.position = to;
        }

        static IEnumerator Scale(Transform t, Vector3 from, Vector3 to, float seconds)
        {
            if (!t) yield break;
            for (float e = 0; e < seconds; e += Time.deltaTime)
            {
                t.localScale = Vector3.Lerp(from, to, Mathf.SmoothStep(0, 1, e / seconds));
                yield return null;
            }
            t.localScale = to;
        }

        static IEnumerator Pulse(Transform t, float amount, float seconds)
        {
            if (!t) yield break;
            var baseScale = t.localScale;
            for (float e = 0; e < seconds; e += Time.deltaTime)
            {
                t.localScale = baseScale * (1f + (amount - 1f) * Mathf.Sin(e / seconds * Mathf.PI));
                yield return null;
            }
            t.localScale = baseScale;
        }
    }
}
