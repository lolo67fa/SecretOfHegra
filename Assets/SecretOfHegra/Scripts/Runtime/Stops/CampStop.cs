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

        [Header("Clay lamp lesson")]
        public Transform lampCard;          // the objective card is pinned here, right beside the lamp
        public GameObject kamkamBubble;     // "Take the lamp…" speech bubble above Kamkam

        float m_CamelTimer = 12f;
        float m_PointTimer;
        bool m_LampHint;

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
            // from the very start of the journey (welcome speech included) until the lamp is picked up, the card
            // beside the lamp says how to grab it and Kamkam points at it; the lamp may be grabbed during the speech
            bool grabLesson = (step == QuestStep.CampWelcome || step == QuestStep.CampGrabLamp) && !State.lampAcquired;
            Game.Objective.SetWorldAnchor(grabLesson ? lampCard : null);
            SetLampHint(grabLesson);
            if (grabLesson && step == QuestStep.CampWelcome) Game.Objective.Show("obj.CampGrabLamp", "ctl.CampGrabLamp");
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
            Game.Objective.SetWorldAnchor(null);
            SetLampHint(false);
        }

        /// <summary>Kamkam stands by the table, points at the lamp and says "Take the lamp…" until it is picked up.</summary>
        void SetLampHint(bool on)
        {
            m_LampHint = on;
            m_PointTimer = 0f;
            if (kamkamBubble) kamkamBubble.SetActive(on);
            if (!on && KamkamGuide.Instance && KamkamGuide.Instance.figure) KamkamGuide.Instance.figure.Play(NpcGesture.Idle, 0.1f);
        }

        void OnLampGrabbed(HegraItem _)
        {
            if (m_LampHint) SetLampHint(false);
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
            if (m_LampHint)
            {
                var kam = KamkamGuide.Instance ? KamkamGuide.Instance.figure : null;
                m_PointTimer -= Time.deltaTime;
                if (kam && Game.Lamp && m_PointTimer <= 0f)
                {
                    m_PointTimer = 4f;
                    kam.Play(NpcGesture.Point, 4.2f, Game.Lamp.transform);
                }
                var cam = Camera.main;
                if (kamkamBubble && cam)
                {
                    var d = kamkamBubble.transform.position - cam.transform.position; d.y = 0f;
                    if (d.sqrMagnitude > 0.0001f) kamkamBubble.transform.rotation = Quaternion.LookRotation(d);
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
