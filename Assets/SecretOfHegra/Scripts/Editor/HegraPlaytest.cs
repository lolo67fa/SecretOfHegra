using System.Text;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace SecretOfHegra.EditorTools
{
    /// <summary>
    /// Play-mode test helpers (Editor only) that drive the game through the same XRI paths the player uses:
    /// teleport requests, interactor select/deselect, and dialogue advancing.
    /// </summary>
    public static class HegraPlaytest
    {
        static GameManager G => GameManager.Instance;

        public static string Status()
        {
            var g = G;
            if (!g || g.State == null) return "no game";
            var s = g.State;
            var sb = new StringBuilder();
            sb.Append($"step={s.step} coins={s.coins} kohl={s.hasKohl}/{s.kohlApplied} name='{s.playerName}' shard={s.shardEngraved} ");
            sb.Append($"signs={s.signInscription}/{s.signWater}/{s.signEagle} assisted={s.assistedRoute} ");
            sb.Append($"dialogue={g.Dialogue.CurrentSequence} beacon={(ObjectiveBeacon.Current ? ObjectiveBeacon.Current.name : "-")} head={g.Rig.Head.position}");
            return sb.ToString();
        }

        /// <summary>Teleports so the player stands at a world point (via the real TeleportationProvider).</summary>
        public static string TeleportTo(Vector3 p)
        {
            var tp = Object.FindAnyObjectByType<TeleportationProvider>();
            tp.QueueTeleportRequest(new TeleportRequest { destinationPosition = p, matchOrientation = MatchOrientation.None });
            return "teleport queued to " + p;
        }

        public static string TeleportToObject(string name, Vector3 offset)
        {
            var go = GameObject.Find(name);
            if (!go) return "not found: " + name;
            var p = go.transform.position + offset;
            p.y = 0f;
            return TeleportTo(p);
        }

        public static string Grab(Component item, bool right = true)
        {
            var g = G;
            var ctrl = right ? g.Rig.RightController : g.Rig.LeftController;
            var nf = ctrl.GetComponentInChildren<NearFarInteractor>();
            var grab = item.GetComponent<XRGrabInteractable>();
            var mgr = Object.FindAnyObjectByType<XRInteractionManager>();
            if (grab.isSelected) mgr.CancelInteractableSelection((IXRSelectInteractable)grab);
            mgr.SelectEnter((IXRSelectInteractor)nf, (IXRSelectInteractable)grab);
            return "grabbed " + item.name + " held=" + grab.isSelected;
        }

        /// <summary>Releases an item at a world position (like letting go of it there).</summary>
        public static string ReleaseAt(Component item, Vector3 worldPos)
        {
            var grab = item.GetComponent<XRGrabInteractable>();
            var mgr = Object.FindAnyObjectByType<XRInteractionManager>();
            if (grab.isSelected) mgr.CancelInteractableSelection((IXRSelectInteractable)grab);
            var rb = item.GetComponent<Rigidbody>();
            if (rb)
            {
                if (!rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
                rb.position = worldPos;
            }
            item.transform.position = worldPos;
            return "released " + item.name + " at " + worldPos;
        }

        public static string SkipDialogue()
        {
            var d = G.Dialogue.CurrentSequence;
            G.Dialogue.SkipSequence();
            return "skipped " + (d ?? "(none)");
        }

        /// <summary>
        /// Writes a checkpoint save in which every step before <paramref name="target"/> is satisfied
        /// (as the stops would have done), then continues the game from it. Tests save/continue too.
        /// </summary>
        public static string JumpTo(QuestStep target, string name = "Lulu")
        {
            var s = GameState.CreateNew(false);
            s.step = QuestFlow.FirstStep;
            int guard = 0;
            while (s.step < target && guard++ < 100)
            {
                var step = s.step;
                switch (step)
                {
                    case QuestStep.CampTakeCoins: s.lampAcquired = true; s.ReceiveStartingCoins(); break;
                    case QuestStep.CampTeleport: s.tutorialTeleported = true; break;
                    case QuestStep.CampSnapTurn: s.tutorialTurned = true; break;
                    case QuestStep.CampStoreLamp: s.lampStoredOnce = true; break;
                    case QuestStep.CampFarewell: s.ReceiveStartingCoins(); break;
                    case QuestStep.MarketBuyKohl: s.BuyKohl(); break;
                    case QuestStep.MarketApplyKohl: s.ApplyKohl(); break;
                    case QuestStep.MarketEnterName: s.SetName(name); break;
                    case QuestStep.MarketEngrave: s.EngraveShard(); break;
                    case QuestStep.TheaterPerformance: s.performanceSeen = true; break;
                    case QuestStep.TheaterFindInscription: s.GrantSign(SignId.Inscription); break;
                    case QuestStep.IthlibPuzzle: s.GrantSign(SignId.Water); break;
                    case QuestStep.BintAlign: s.GrantSign(SignId.Eagle); break;
                    case QuestStep.FaridInsertShard: s.shardInserted = true; break;
                    case QuestStep.FaridDoorOpening: s.doorOpened = true; break;
                    case QuestStep.FaridOpenChest: s.chestOpened = true; break;
                }
                if (!QuestFlow.TryComplete(s, step)) return "could not pass " + step;
            }
            G.Saves.SaveProgress(s);
            G.RequestContinue();
            return "continuing at " + s.step;
        }

        /// <summary>Play Mode shortcut to try Sign 1: writes a checkpoint at the Elephant Rock step (replaces the current save).</summary>
        [UnityEditor.MenuItem("Tools/Secret of Hegra/Play Mode: Jump to Elephant Rock", priority = 40)]
        static void JumpToElephantRockMenu() => Debug.Log("[Hegra] " + JumpTo(QuestStep.TheaterFindInscription));

        [UnityEditor.MenuItem("Tools/Secret of Hegra/Play Mode: Jump to Elephant Rock", true)]
        static bool JumpToElephantRockValidate() => Application.isPlaying && G;

        // ───────── walking measurement through the real ContinuousMoveProvider ─────────

        static Vector3 s_WalkStart;
        static float s_WalkStartTime;
        static UnityEngine.XR.Interaction.Toolkit.Inputs.Readers.XRInputValueReader.InputSourceMode s_PrevMode;

        public static string StartWalk(float speedOverride = -1f, bool sprint = false)
        {
            var move = Object.FindAnyObjectByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ContinuousMoveProvider>();
            if (!move) return "no move provider";
            G.Rig.SetSprint(sprint);
            if (speedOverride > 0f) move.moveSpeed = speedOverride;
            var r = move.leftHandMoveInput;
            s_PrevMode = r.inputSourceMode;
            r.inputSourceMode = UnityEngine.XR.Interaction.Toolkit.Inputs.Readers.XRInputValueReader.InputSourceMode.ManualValue;
            r.manualValue = new Vector2(0f, 1f);
            s_WalkStart = G.Rig.Head.position;
            s_WalkStartTime = Time.time;
            return "walking at moveSpeed=" + move.moveSpeed;
        }

        public static string StopWalk()
        {
            var move = Object.FindAnyObjectByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ContinuousMoveProvider>();
            var r = move.leftHandMoveInput;
            r.manualValue = Vector2.zero;
            r.inputSourceMode = s_PrevMode;
            var d = G.Rig.Head.position - s_WalkStart; d.y = 0;
            float t = Time.time - s_WalkStartTime;
            G.Rig.SetSprint(false);
            return $"walked {d.magnitude:0.00} m in {t:0.00} s = {d.magnitude / Mathf.Max(0.01f, t):0.00} m/s (moveSpeed={move.moveSpeed:0.00})";
        }

        public static T Stop<T>() where T : StopBase => Object.FindAnyObjectByType<T>();

        // ───────── journey bot: one natural action for the current step per call ─────────

        static XRInteractionManager Mgr => Object.FindAnyObjectByType<XRInteractionManager>();
        static NearFarInteractor RightHand => G.Rig.RightController.GetComponentInChildren<NearFarInteractor>();

        static void Tap(XRBaseInteractable target)
        {
            Mgr.SelectEnter((IXRSelectInteractor)RightHand, (IXRSelectInteractable)target);
            Mgr.SelectExit((IXRSelectInteractor)RightHand, (IXRSelectInteractable)target);
        }

        static void Invoke(object o, string method) =>
            o.GetType().GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public).Invoke(o, null);

        static string Near(Transform t, float back = 2f)
        {
            if (!t) return "no target";
            var d = t.position - G.Rig.Head.position; d.y = 0;
            if (d.magnitude <= back + 0.5f) return null;
            var p = t.position - d.normalized * back; p.y = 0;
            TeleportTo(p);
            return "teleport near " + t.name;
        }

        static float s_TurnUntil, s_TurnRearm;

        [UnityEditor.InitializeOnEnterPlayMode]
        static void ResetStatics() { s_TurnUntil = 0f; s_TurnRearm = 0f; } // play mode runs without a domain reload

        /// <summary>
        /// Performs the action a player would take at the current step (grab, socket, teleport, snap turn,
        /// lamp, gates, gaze…), or advances dialogue while it is playing. Call repeatedly with short waits.
        /// </summary>
        public static string AutoStep()
        {
            var g = G;
            if (!g || g.State == null || !g.InGame) return "not in game";
            var turn = Object.FindAnyObjectByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.SnapTurnProvider>();
            if (turn && s_TurnUntil > 0 && Time.time > s_TurnUntil)
            {
                turn.rightHandTurnInput.manualValue = Vector2.zero;
                turn.rightHandTurnInput.inputSourceMode = UnityEngine.XR.Interaction.Toolkit.Inputs.Readers.XRInputValueReader.InputSourceMode.InputActionReference;
                s_TurnUntil = 0;
                s_TurnRearm = Time.time + 1f; // the provider ends the turn only after a frame of zero input
            }
            if (g.Dialogue.IsPlaying) { g.Dialogue.Advance(); return "advance " + g.Dialogue.CurrentSequence; }

            var step = g.State.step;
            var market = Stop<MarketStop>();
            switch (step)
            {
                case QuestStep.CampTakeCoins:
                {
                    var camp = Stop<CampStop>();
                    if (camp.coinPlate) { Tap(camp.coinPlate); return "take coins from the plate"; }
                    return "no coin plate";
                }
                case QuestStep.CampTeleport:
                {
                    var m = Stop<CampStop>().teleportMarker.position; m.y = 0;
                    return TeleportTo(m);
                }
                case QuestStep.CampSnapTurn:
                    if (!turn) return "no snap turn provider";
                    if (s_TurnUntil > 0 || Time.time < s_TurnRearm) return "turning";
                    turn.rightHandTurnInput.inputSourceMode = UnityEngine.XR.Interaction.Toolkit.Inputs.Readers.XRInputValueReader.InputSourceMode.ManualValue;
                    turn.rightHandTurnInput.manualValue = new Vector2(1f, 0f);
                    s_TurnUntil = Time.time + 0.4f;
                    return "snap turn (right stick)";
                case QuestStep.CampStoreLamp:
                {
                    // bring the carrying hand's lamp to the waist, as a player would
                    var carry = g.Rig.GetComponent<LampHandCarry>();
                    if (carry && carry.IsCarrying) { carry.StoreOnBelt(); return "lamp brought to the belt"; }
                    return "belt holds lamp=" + g.BeltSocket.ForceInsert(g.Lamp);
                }
                case QuestStep.MarketMeetKamkam:
                    return Near(KamkamGuide.Instance ? KamkamGuide.Instance.transform : market.kamkamStall) ?? "waiting for Kamkam";
                case QuestStep.MarketBuyKohl:
                    if (market.coin && market.coin.gameObject.activeInHierarchy && !market.coin.IsHeldByHand)
                    {
                        Grab(market.coin);
                        return ReleaseAt(market.coin, market.payTray.transform.position + Vector3.up * 0.03f);
                    }
                    return "waiting for payment";
                case QuestStep.MarketApplyKohl:
                    Invoke(market, "ApplyKohl"); return "apply kohl";
                case QuestStep.MarketMeetWahballahi:
                    return Near(market.wahballahi ? market.wahballahi.transform : null, 1.6f) ?? "waiting for Wahballahi";
                case QuestStep.MarketEnterName:
                    if (market.Keyboard && market.Keyboard.IsOpen) { market.Keyboard.SubmitForTest("Lulu"); return "name submitted"; }
                    return "waiting for keyboard";
                case QuestStep.MarketEngrave:
                    if (market.engraver) market.engraver.CarveAll(); return "carve all";
                case QuestStep.TheaterIntro:
                    return Near(Stop<TheaterStop>().stageCenter, 4f) ?? "waiting at stage";
                case QuestStep.TheaterPerformance:
                    g.Dialogue.SkipSequence(); return "skip performance";
                case QuestStep.TheaterFindInscription:
                {
                    var st = Stop<TheaterStop>();
                    var target = st.inscription.transform;
                    var far = Near(target, 1.2f);
                    if (far != null) return far;
                    if (st.rock && st.rock.Revealed)
                    {
                        if (st.rock.Showing) return "watching the glyph sequence";
                        int next = st.rock.Expected;
                        if (next < 0) return "rock solved";
                        Tap(st.rock.glyphButtons[next]);
                        return "select glyph " + next;
                    }
                    if (!g.Lamp.IsHeldByHand) Grab(g.Lamp);
                    g.Lamp.looseReturnSeconds = 0f; // automation only: keep it at the face instead of returning to the belt
                    var r = ReleaseAt(g.Lamp, target.position - target.forward * 0.5f);
                    var rb = g.Lamp.GetComponent<Rigidbody>();
                    if (rb) rb.isKinematic = true; // held up to the rock face rather than dropped
                    return r + " (lamp held at rock face)";
                }
                case QuestStep.IthlibIntro:
                    if (g.Lamp.looseReturnSeconds <= 0f) { g.Lamp.looseReturnSeconds = 2f; g.Lamp.Recover(); }
                    return Near(Stop<IthlibStop>().puzzleCenter, 3f) ?? "waiting at puzzle";
                case QuestStep.IthlibPuzzle:
                {
                    var st = Stop<IthlibStop>();
                    var solved = new[] { 3, 0, 1 };
                    for (int i = 0; i < st.gates.Count; i++)
                        if (st.gates[i].quarterTurns != solved[i] && !st.Solved)
                        { Tap(st.gates[i].GetComponent<XRSimpleInteractable>()); return "turn " + st.gates[i].name; }
                    return "gates set";
                }
                case QuestStep.BintIntro:
                    return Near(Stop<BintStop>().watchingStone, 0.01f) ?? "on the stone";
                case QuestStep.BintAlign:
                {
                    var st = Stop<BintStop>();
                    var far = Near(st.watchingStone, 0.01f);
                    if (far != null) return far;
                    var aim = st.EagleAwake ? st.stars[st.correctStar] : st.eagle;
                    st.debugGazeDirection = aim.position - g.Rig.Head.position;
                    return "gaze at " + aim.name;
                }
                case QuestStep.FaridIntro:
                    Stop<BintStop>().debugGazeDirection = null;
                    return Near(Stop<FaridStop>().facadeCenter, 3f) ?? "waiting at facade";
                case QuestStep.FaridInsertShard:
                {
                    var st = Stop<FaridStop>();
                    var shard = NameShard.Instance;
                    if (st.niche.Holds(shard.Item)) return "shard in niche";
                    var far = Near(st.niche.transform, 1.2f);
                    if (far != null) return far;
                    Grab(shard);
                    return ReleaseAt(shard, st.niche.transform.position + Vector3.up * 0.12f);
                }
                case QuestStep.FaridOpenChest:
                {
                    var st = Stop<FaridStop>();
                    var far = Near(st.lidHandle.transform, 1.2f);
                    if (far != null) return far;
                    Tap(st.lidHandle); return "lift lid";
                }
                case QuestStep.Complete:
                    return "COMPLETE";
            }
            return "waiting (" + step + ")";
        }
    }
}
