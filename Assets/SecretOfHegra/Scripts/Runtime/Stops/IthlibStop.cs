using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SecretOfHegra
{
    /// <summary>
    /// Stop 4 — Jabal Ithlib water puzzle. Turning the stone gates routes the spring through the channels;
    /// when the water reaches the cistern, it fills, the passage slab slides aside and Sign 2 is granted.
    /// </summary>
    public class IthlibStop : StopBase
    {
        [System.Serializable]
        public class CellView
        {
            public int x, y;
            public GameObject waterN, waterE, waterS, waterW, pool;
        }

        public Transform puzzleCenter;
        public List<ChannelGate> gates = new List<ChannelGate>();   // order: GateA, GateB, GateC
        public List<CellView> cells = new List<CellView>();
        public Transform cisternWater;
        public Transform cisternLow, cisternHigh;
        public Transform passageSlab;
        public Vector3 passageOpenOffset = new Vector3(2.2f, 0, 0);
        public Transform spillMarker;
        public AudioSource spillSound;
        public XRSimpleInteractable resetLever;
        public Transform toBint;
        public float arriveDistance = 8f;

        WaterNetwork m_Net;
        readonly string[] m_GateIds = { "GateA", "GateB", "GateC" };
        readonly Dictionary<string, (int x, int y)> m_GateCells = new Dictionary<string, (int, int)>();
        bool m_Solved, m_IntroStarted;
        Vector3 m_SlabClosed;

        public bool Solved => m_Solved;
        public WaterNetwork Network => m_Net;

        public override void Initialize()
        {
            m_Net = WaterNetwork.CreateIthlib();
            for (int x = 0; x < m_Net.width; x++)
                for (int y = 0; y < m_Net.height; y++)
                    if (m_Net[x, y] != null && m_Net[x, y].rotatable) m_GateCells[m_Net[x, y].id] = (x, y);
            for (int i = 0; i < gates.Count && i < m_GateIds.Length; i++)
            {
                var c = m_GateCells[m_GateIds[i]];
                gates[i].SetQuarterTurns(m_Net[c.x, c.y].rotation, false);
                gates[i].Turned += OnGateTurned;
            }
            if (passageSlab) m_SlabClosed = passageSlab.localPosition;
            if (resetLever) resetLever.selectEntered.AddListener(_ => { if (!m_Solved) ResetPuzzle(); });
            Refresh();
        }

        public override void RestoreState(GameState s)
        {
            if (s.signWater) SetSolvedInstant();
        }

        public override void OnStepEntered(QuestStep step)
        {
            switch (step)
            {
                case QuestStep.IthlibIntro:
                    ObjectiveBeacon.Current = puzzleCenter;
                    m_IntroStarted = false;
                    break;
                case QuestStep.IthlibPuzzle:
                    ObjectiveBeacon.Current = FirstWrongGate();
                    Game.Objective.SetActions(("menu.reset_puzzle", ResetPuzzle));
                    break;
            }
        }

        void Update()
        {
            if (Game == null || Game.State == null || !Game.InGame) return;
            if (IsCurrent(QuestStep.IthlibIntro) && !m_IntroStarted && puzzleCenter)
            {
                var d = Game.Rig.Head.position - puzzleCenter.position; d.y = 0;
                if (d.magnitude < arriveDistance)
                {
                    m_IntroStarted = true;
                    Game.Dialogue.Play("ithlib_intro", () => Complete(QuestStep.IthlibIntro));
                }
            }
            // strongest hint: correctly-set gates glow
            bool showCorrect = IsCurrent(QuestStep.IthlibPuzzle) && Game.State.hintLevel >= 3;
            for (int i = 0; i < gates.Count; i++)
                if (gates[i].correctGlow) gates[i].correctGlow.enabled = showCorrect && GateCorrect(i);
        }

        bool GateCorrect(int i)
        {
            var c = m_GateCells[m_GateIds[i]];
            var cell = m_Net[c.x, c.y];
            return WaterNetwork.Rotate(cell.baseMask, cell.rotation) == WaterNetwork.Rotate(cell.baseMask, cell.solvedRotation);
        }

        Transform FirstWrongGate()
        {
            for (int i = 0; i < gates.Count; i++) if (!GateCorrect(i)) return gates[i].transform;
            return puzzleCenter;
        }

        void OnGateTurned(ChannelGate gate)
        {
            if (m_Solved) { gate.SetQuarterTurns(m_Net[m_GateCells[m_GateIds[gates.IndexOf(gate)]].x, m_GateCells[m_GateIds[gates.IndexOf(gate)]].y].rotation, false); return; }
            int i = gates.IndexOf(gate);
            if (i < 0) return;
            var c = m_GateCells[m_GateIds[i]];
            m_Net[c.x, c.y].rotation = gate.quarterTurns;
            Refresh();
            if (IsCurrent(QuestStep.IthlibPuzzle)) ObjectiveBeacon.Current = FirstWrongGate();
        }

        void Refresh()
        {
            var flow = m_Net.Flow();
            foreach (var v in cells)
            {
                flow.wetPorts.TryGetValue((v.x, v.y), out int ports);
                bool wet = flow.wet.Contains((v.x, v.y));
                var cell = m_Net[v.x, v.y];
                // views of rotatable cells sit on the rotor, so they use the unrotated (base) ports
                int mask = cell != null && cell.rotatable ? cell.baseMask : ports;
                Set(v.waterN, wet && (mask & WaterNetwork.N) != 0);
                Set(v.waterE, wet && (mask & WaterNetwork.E) != 0);
                Set(v.waterS, wet && (mask & WaterNetwork.S) != 0);
                Set(v.waterW, wet && (mask & WaterNetwork.W) != 0);
                Set(v.pool, wet);
            }
            if (spillMarker)
            {
                spillMarker.gameObject.SetActive(!flow.solved);
                var stopView = cells.Find(v => v.x == flow.stopCell.x && v.y == flow.stopCell.y);
                var anchor = stopView != null && stopView.pool ? stopView.pool.transform : null;
                if (anchor) spillMarker.position = anchor.position + Vector3.up * 0.05f;
                if (spillSound) spillSound.transform.position = spillMarker.position;
            }
            if (flow.solved && !m_Solved) OnSolved();
        }

        static void Set(GameObject g, bool on) { if (g && g.activeSelf != on) g.SetActive(on); }

        void OnSolved()
        {
            m_Solved = true;
            foreach (var g in gates) g.GetComponent<XRSimpleInteractable>().enabled = false;
            StartCoroutine(FillAndOpen(4f));
            Game.Dialogue.Play("ithlib_done");
            Game.GrantSign(SignId.Water);
            ObjectiveBeacon.Current = toBint;
        }

        IEnumerator FillAndOpen(float seconds)
        {
            HegraAudio.PlayAt(Sfx.StoneDoor, passageSlab ? passageSlab.position : transform.position, 0.8f);
            var from = cisternWater ? cisternWater.position : Vector3.zero;
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / seconds);
                if (cisternWater) cisternWater.position = Vector3.Lerp(from, cisternHigh.position, k);
                if (passageSlab) passageSlab.localPosition = m_SlabClosed + passageOpenOffset * k;
                yield return null;
            }
            SetFinalPositions();
        }

        void SetFinalPositions()
        {
            if (cisternWater && cisternHigh) cisternWater.position = cisternHigh.position;
            if (passageSlab) passageSlab.localPosition = m_SlabClosed + passageOpenOffset;
        }

        void SetSolvedInstant()
        {
            for (int i = 0; i < gates.Count; i++)
            {
                var c = m_GateCells[m_GateIds[i]];
                m_Net[c.x, c.y].rotation = m_Net[c.x, c.y].solvedRotation;
                gates[i].SetQuarterTurns(m_Net[c.x, c.y].rotation, false);
                gates[i].GetComponent<XRSimpleInteractable>().enabled = false;
            }
            m_Solved = true;
            Refresh();
            SetFinalPositions();
        }

        public override bool CanResetPuzzle => IsCurrent(QuestStep.IthlibPuzzle) && !m_Solved;

        public override void ResetPuzzle()
        {
            if (m_Solved) return;
            var fresh = WaterNetwork.CreateIthlib();
            for (int i = 0; i < gates.Count; i++)
            {
                var c = m_GateCells[m_GateIds[i]];
                m_Net[c.x, c.y].rotation = fresh[c.x, c.y].rotation;
                gates[i].SetQuarterTurns(fresh[c.x, c.y].rotation, false);
            }
            Refresh();
            Game.Menus.ShowToast("toast.puzzle_reset");
        }
    }
}
