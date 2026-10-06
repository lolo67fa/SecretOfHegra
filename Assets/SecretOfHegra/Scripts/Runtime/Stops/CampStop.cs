using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// Stop 1 — Caravan Camp. Shuʿayd teaches grabbing the clay lamp, teleporting, snap turning and the belt
    /// hook. Each lesson completes when the player actually does it; every lesson has a skip.
    /// </summary>
    public class CampStop : StopBase
    {
        public NpcFigure shuayd;
        public Transform teleportMarker;
        public Transform marketDirection;
        public Transform camel;
        public GameObject markerVisual;

        float m_CamelTimer = 12f;

        public override void Initialize()
        {
            Game.Rig.Teleported += OnTeleported;
            Game.Rig.SnapTurned += OnSnapTurned;
            if (Game.Lamp) Game.Lamp.Grabbed += OnLampGrabbed;
            if (Game.BeltSocket) Game.BeltSocket.ItemInserted += OnBeltInserted;
            if (markerVisual) markerVisual.SetActive(false);
        }

        void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (!gm) return;
            if (gm.Rig) { gm.Rig.Teleported -= OnTeleported; gm.Rig.SnapTurned -= OnSnapTurned; }
            if (gm.Lamp) gm.Lamp.Grabbed -= OnLampGrabbed;
            if (gm.BeltSocket) gm.BeltSocket.ItemInserted -= OnBeltInserted;
        }

        public override void OnStepEntered(QuestStep step)
        {
            if (markerVisual) markerVisual.SetActive(step == QuestStep.CampTeleport);
            switch (step)
            {
                case QuestStep.CampWelcome:
                    Game.Objective.SetActions(("ui.skip_tutorial", SkipTutorial));
                    if (shuayd) shuayd.Play(NpcGesture.Wave, 3f);
                    Game.Dialogue.Play("camp_welcome", () => Complete(QuestStep.CampWelcome));
                    break;

                case QuestStep.CampGrabLamp:
                    ObjectiveBeacon.Current = Game.Lamp ? Game.Lamp.transform : null;
                    Game.Objective.SetActions(("ui.skip_tutorial", SkipTutorial));
                    Game.Dialogue.Play("camp_grab");
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
                    if (State.ReceiveStartingCoins())
                    {
                        HegraAudio.Play2D(Sfx.Coin);
                        Game.Menus.ShowToast("toast.coins_received");
                        Game.SaveNow();
                    }
                    if (shuayd) shuayd.Play(NpcGesture.Point, 4f, marketDirection);
                    Game.Dialogue.Play(State.tutorialSkipped ? "camp_farewell_short" : "camp_farewell", () => Complete(QuestStep.CampFarewell));
                    break;
            }
        }

        public override void OnStepExited(QuestStep step)
        {
            if (markerVisual) markerVisual.SetActive(false);
        }

        void OnLampGrabbed(HegraItem _)
        {
            if (Game.State == null || Game.State.lampAcquired) return;
            Game.State.lampAcquired = true;
            Game.NotifyStateChanged();
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
            if (Game.Lamp && Game.BeltSocket)
            {
                Game.State.lampAcquired = true;
                Game.BeltSocket.ForceInsert(Game.Lamp);
                if (!Game.State.lampStoredOnce) { Game.State.lampStoredOnce = true; Game.NotifyStateChanged(); }
            }
        }

        void SkipTutorial()
        {
            Game.Dialogue.StopAll();
            Game.SkipTutorial();
        }

        void Update()
        {
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
