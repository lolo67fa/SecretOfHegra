using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SecretOfHegra
{
    /// <summary>
    /// Stop 1 — Caravan Camp. The player starts with the clay lamp already in the dominant hand. Shuʿayd hands over
    /// the three coins (from the plate on the table in front of him), then teaches teleporting, snap turning and
    /// hanging the lamp on the belt hook. Each lesson completes when the player actually does it; every lesson has
    /// a skip.
    /// </summary>
    public class CampStop : StopBase
    {
        public NpcFigure shuayd;
        public Transform teleportMarker;
        public Transform marketDirection;
        public Transform camel;
        public GameObject markerVisual;

        [Header("Coins lesson")]
        public XRSimpleInteractable coinPlate;  // point + trigger/grip (or reach and grip) takes the coins
        public GameObject coinPile;             // the three coins on the plate, hidden once taken
        public Transform coinCard;              // the objective card is pinned here, beside the coins

        float m_CamelTimer = 12f;
        float m_PointTimer;
        bool m_CoinsHint;

        public override void Initialize()
        {
            Game.Rig.Teleported += OnTeleported;
            Game.Rig.SnapTurned += OnSnapTurned;
            if (Game.BeltSocket) Game.BeltSocket.ItemInserted += OnBeltInserted;
            if (coinPlate) coinPlate.selectEntered.AddListener(_ => TakeCoins());
            if (markerVisual) markerVisual.SetActive(false);
        }

        void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (!gm) return;
            if (gm.Rig) { gm.Rig.Teleported -= OnTeleported; gm.Rig.SnapTurned -= OnSnapTurned; }
            if (gm.BeltSocket) gm.BeltSocket.ItemInserted -= OnBeltInserted;
        }

        public override void RestoreState(GameState s)
        {
            if (coinPile) coinPile.SetActive(!s.coinsReceived);
        }

        public override void OnStepEntered(QuestStep step)
        {
            if (markerVisual) markerVisual.SetActive(step == QuestStep.CampTeleport);
            // from the very start of the journey (welcome speech included) until the coins are taken, the card
            // beside the coin plate says how to take them and Shuʿayd points at the plate; they may be taken early
            bool coinsLesson = (step == QuestStep.CampWelcome || step == QuestStep.CampTakeCoins) && !State.coinsReceived;
            Game.Objective.SetWorldAnchor(coinsLesson ? coinCard : null);
            SetCoinsHint(coinsLesson);
            if (coinsLesson && step == QuestStep.CampWelcome) Game.Objective.Show("obj.CampTakeCoins", "ctl.CampTakeCoins");
            switch (step)
            {
                case QuestStep.CampWelcome:
                    Game.Objective.SetActions(("ui.take_coins", TakeCoins), ("ui.skip_tutorial", SkipTutorial));
                    if (shuayd) shuayd.Play(NpcGesture.Wave, 3f);
                    Game.Dialogue.Play("camp_welcome", () => Complete(QuestStep.CampWelcome));
                    break;

                case QuestStep.CampTakeCoins:
                    ObjectiveBeacon.Current = coinPlate ? coinPlate.transform : null;
                    Game.Objective.SetActions(("ui.take_coins", TakeCoins), ("ui.skip_tutorial", SkipTutorial));
                    Game.Dialogue.Play("camp_coins");
                    break;

                case QuestStep.CampTeleport:
                    ObjectiveBeacon.Current = teleportMarker;
                    Game.Objective.SetActions(("ui.skip_tutorial", SkipTutorial));
                    Game.Dialogue.Play("camp_teleport");
                    break;

                case QuestStep.CampSnapTurn:
                    ObjectiveBeacon.Current = shuayd ? shuayd.transform : null;
                    Game.Objective.SetActions(("ui.skip_tutorial", SkipTutorial));
                    Game.Dialogue.Play("camp_turn");
                    break;

                case QuestStep.CampStoreLamp:
                    ObjectiveBeacon.Current = Game.BeltSocket ? Game.BeltSocket.transform : null;
                    Game.Objective.SetActions(("ui.store_for_me", StoreLampForMe), ("ui.skip_tutorial", SkipTutorial));
                    Game.Dialogue.Play("camp_store");
                    break;

                case QuestStep.CampFarewell:
                    ObjectiveBeacon.Current = marketDirection;
                    Game.Objective.ClearButtons();
                    GiveCoins(); // only if the coins lesson was skipped
                    if (shuayd) shuayd.Play(NpcGesture.Point, 4f, marketDirection);
                    Game.Dialogue.Play(State.tutorialSkipped ? "camp_farewell_short" : "camp_farewell", () => Complete(QuestStep.CampFarewell));
                    break;
            }
        }

        public override void OnStepExited(QuestStep step)
        {
            if (markerVisual) markerVisual.SetActive(false);
            Game.Objective.SetWorldAnchor(null);
            SetCoinsHint(false);
        }

        /// <summary>Shuʿayd points at the coin plate until the coins are taken.</summary>
        void SetCoinsHint(bool on)
        {
            m_CoinsHint = on;
            m_PointTimer = 3f;   // let the welcome wave finish first
            if (!on && shuayd) shuayd.Play(NpcGesture.Idle, 0.1f);
        }

        void TakeCoins()
        {
            if (Game.State == null || !Game.InGame || Game.State.coinsReceived) return;
            if (!GiveCoins()) return;
            SetCoinsHint(false);
            Game.NotifyStateChanged();   // advances "take the coins" (also when taken during the welcome)
        }

        bool GiveCoins()
        {
            if (!State.ReceiveStartingCoins()) return false;
            HegraAudio.PlayAt(Sfx.Coin, coinPlate ? coinPlate.transform.position : transform.position);
            Game.Menus.ShowToast("toast.coins_received");
            if (coinPile) coinPile.SetActive(false);
            Game.SaveNow();
            return true;
        }

        void OnTeleported()
        {
            if (Game.State == null || !IsCurrent(QuestStep.CampTeleport)) return;
            Game.State.tutorialTeleported = true;
            Game.NotifyStateChanged();
        }

        void OnSnapTurned()
        {
            if (Game.State == null || !IsCurrent(QuestStep.CampSnapTurn)) return;
            Game.State.tutorialTurned = true;
            Game.NotifyStateChanged();
        }

        void OnBeltInserted(HegraItem item)
        {
            if (Game.State == null || item.kind != ItemKind.Lamp || Game.State.lampStoredOnce) return;
            if (!Game.InGame || Game.State.step < QuestStep.CampStoreLamp) return;
            Game.State.lampStoredOnce = true;
            Game.NotifyStateChanged();
        }

        void StoreLampForMe()
        {
            if (Game.BeltSocket && Game.Lamp && Game.BeltSocket.Holds(Game.Lamp))
            {
                // already hanging there: just count the lesson
                if (!Game.State.lampStoredOnce) { Game.State.lampStoredOnce = true; Game.NotifyStateChanged(); }
                return;
            }
            Game.PutLampOnBelt();   // the belt insertion completes the lesson (OnBeltInserted)
        }

        void SkipTutorial()
        {
            Game.Dialogue.StopAll();
            Game.SkipTutorial();
        }

        void Update()
        {
            if (m_CoinsHint && shuayd && coinPlate)
            {
                m_PointTimer -= Time.deltaTime;
                if (m_PointTimer <= 0f)
                {
                    m_PointTimer = 4f;
                    shuayd.Play(NpcGesture.Point, 4.2f, coinPlate.transform);
                }
            }

            if (!camel) return;
            m_CamelTimer -= Time.deltaTime;
            if (m_CamelTimer <= 0f)
            {
                m_CamelTimer = Random.Range(18f, 35f);
                HegraAudio.PlayAt(Sfx.Camel, camel.position + Vector3.up * 1.6f, 0.7f);
            }
        }
    }
}
