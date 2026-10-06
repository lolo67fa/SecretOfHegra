using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// Stop 2 — Market. Kamkam tells the legend and sells kohl (one coin, configurable; paid once), the
    /// kohl is applied in front of the face, then Wahballahi carves the player's name on a clay shard
    /// that becomes the key for the finale.
    /// </summary>
    public class MarketStop : StopBase
    {
        [Header("Kamkam")]
        public Transform kamkamStall;
        public PayTray payTray;
        public HegraItem coin;
        public Transform coinDish;
        public HegraItem kohlJar;
        public KohlApplicator kohl;
        public BronzeMirror mirror;
        [Min(0)] public int kohlPrice = GameState.KohlPrice;
        public float talkDistance = 3.2f;

        [Header("Wahballahi")]
        public NpcFigure wahballahi;
        public Transform keyboardAnchor;
        public ShardEngraver engraver;
        public HegraItem chisel;
        public NameShard shard;
        public Transform shardBenchPose;

        NameKeyboard m_Keyboard;
        bool m_GreetPlayed;

        public NameKeyboard Keyboard => m_Keyboard;

        public override void Initialize()
        {
            m_Keyboard = NameKeyboard.Create(transform, Game.Rig.Camera);
            if (payTray) payTray.CoinPlaced += OnCoinPlaced;
            if (kohl) kohl.Applied += ApplyKohl;
            if (engraver) engraver.Finished += OnEngraved;
            SetShardGrabbable(false);
            if (coin) coin.gameObject.SetActive(false);
            if (kohlJar) kohlJar.gameObject.SetActive(false);
        }

        public override void RestoreState(GameState s)
        {
            if (kohlJar) kohlJar.gameObject.SetActive(s.hasKohl && !s.kohlApplied);
            if (coin) coin.gameObject.SetActive(false);
            if (mirror) mirror.Redraw(s.kohlApplied, Game.Settings.appearance);
            if (shard)
            {
                if (s.shardEngraved)
                {
                    shard.Inscribe(s.playerName, s.shardId, 1f);
                    SetShardGrabbable(true);
                    if (!s.shardInserted && Game.SatchelSocket) Game.SatchelSocket.ForceInsert(shard.Item);
                }
                else
                {
                    shard.Inscribe("", "", 0f);
                    PlaceShardOnBench();
                }
            }
        }

        public override void OnStepEntered(QuestStep step)
        {
            switch (step)
            {
                case QuestStep.MarketMeetKamkam:
                    ObjectiveBeacon.Current = KamkamGuide.Instance ? KamkamGuide.Instance.transform : kamkamStall;
                    m_GreetPlayed = false;
                    break;

                case QuestStep.MarketLegend:
                    ObjectiveBeacon.Current = KamkamGuide.Instance ? KamkamGuide.Instance.transform : kamkamStall;
                    Game.Dialogue.Play("market_legend", () => Complete(QuestStep.MarketLegend));
                    break;

                case QuestStep.MarketBuyKohl:
                    ObjectiveBeacon.Current = payTray ? payTray.transform : kamkamStall;
                    OfferCoin();
                    Game.Objective.SetActions(("ui.pay_coin", PayForMe));
                    Game.Dialogue.Play("market_buy");
                    break;

                case QuestStep.MarketApplyKohl:
                    if (kohlJar) { kohlJar.gameObject.SetActive(true); ObjectiveBeacon.Current = kohlJar.transform; }
                    if (kohl) kohl.armed = true;
                    Game.Objective.SetActions(("ui.apply_kohl", ApplyKohl));
                    Game.Dialogue.Play("market_bought");
                    break;

                case QuestStep.MarketMeetWahballahi:
                    ObjectiveBeacon.Current = wahballahi ? wahballahi.transform : null;
                    break;

                case QuestStep.MarketEnterName:
                    ObjectiveBeacon.Current = keyboardAnchor;
                    OpenKeyboard();
                    break;

                case QuestStep.MarketEngrave:
                    ObjectiveBeacon.Current = shard ? shard.transform : null;
                    StartEngraving();
                    Game.Objective.SetActions(("ui.carve_for_me", () => { if (engraver) engraver.CarveAll(); }));
                    Game.Dialogue.Play("market_engrave_start");
                    break;

                case QuestStep.MarketReceiveShard:
                    ObjectiveBeacon.Current = shard ? shard.transform : null;
                    SetShardGrabbable(true);
                    if (wahballahi) wahballahi.Play(NpcGesture.Talk, 4f);
                    Game.Dialogue.Play("market_engrave_done", () =>
                    {
                        if (shard && !shard.Item.IsHeldByHand && Game.SatchelSocket) Game.SatchelSocket.ForceInsert(shard.Item);
                        Game.Menus.ShowToast("toast.shard_received");
                        Complete(QuestStep.MarketReceiveShard);
                    });
                    break;
            }
        }

        public override void OnStepExited(QuestStep step)
        {
            if (m_Keyboard && m_Keyboard.IsOpen && Game.State.step != QuestStep.MarketEnterName) m_Keyboard.Close();
        }

        void Update()
        {
            if (Game == null || Game.State == null || !Game.InGame) return;
            var head = Game.Rig.Head.position;
            // measure against her stall spot (not her current position: she may still be elsewhere)
            if (IsCurrent(QuestStep.MarketMeetKamkam) && !m_GreetPlayed && guidePoint &&
                FlatDistance(head, guidePoint.position) < talkDistance + 0.8f)
            {
                m_GreetPlayed = true;
                Game.Dialogue.Play("market_greet", () => Complete(QuestStep.MarketMeetKamkam));
            }
            if (IsCurrent(QuestStep.MarketMeetWahballahi) && wahballahi && !Game.Dialogue.IsPlaying &&
                FlatDistance(head, wahballahi.transform.position) < talkDistance && !m_LessonStarted)
            {
                m_LessonStarted = true;
                wahballahi.Play(NpcGesture.Talk, 6f);
                Game.Dialogue.Play("market_lesson", () => Complete(QuestStep.MarketMeetWahballahi));
            }
        }

        bool m_LessonStarted;

        static float FlatDistance(Vector3 a, Vector3 b) { a.y = 0; b.y = 0; return Vector3.Distance(a, b); }

        // ───────── coin & kohl ─────────

        void OfferCoin()
        {
            if (!coin || Game.State.hasKohl) return;
            coin.gameObject.SetActive(Game.State.CanAfford(kohlPrice) && kohlPrice > 0);
            if (coinDish) coin.transform.SetPositionAndRotation(coinDish.position + Vector3.up * 0.02f, coinDish.rotation);
            coin.SetSpawnPose(coin.transform.position, coin.transform.rotation);
            if (payTray) { payTray.watchedCoin = coin; payTray.accepting = true; }
        }

        void OnCoinPlaced(HegraItem c)
        {
            if (!IsCurrent(QuestStep.MarketBuyKohl)) return;
            c.gameObject.SetActive(false);
            Pay();
        }

        void PayForMe()
        {
            if (!IsCurrent(QuestStep.MarketBuyKohl)) return;
            if (coin) coin.gameObject.SetActive(false);
            if (payTray) payTray.accepting = false;
            Pay();
        }

        void Pay()
        {
            bool afford = Game.State.CanAfford(kohlPrice);
            if (!Game.State.BuyKohl(kohlPrice)) return; // already bought: never charge twice
            HegraAudio.PlayAt(Sfx.Coin, payTray ? payTray.transform.position : transform.position);
            Game.Menus.ShowToast(afford ? "toast.kohl_bought" : "toast.kohl_gift");
            Game.NotifyStateChanged();
        }

        void ApplyKohl()
        {
            if (Game.State == null || Game.State.kohlApplied) return;
            if (!Game.State.hasKohl) Game.State.BuyKohl(0);
            Game.State.ApplyKohl();
            if (kohl) kohl.armed = false;
            HegraAudio.Play2D(Sfx.KohlApply);
            KohlEffect.SetApplied(true, false);
            if (mirror) mirror.Redraw(true, Game.Settings.appearance);
            Game.Menus.ShowToast("toast.kohl_applied");
            Game.Dialogue.Play("market_kohl_done");
            if (kohlJar)
            {
                if (kohlJar.Grab.isSelected && kohlJar.Grab.interactionManager)
                    kohlJar.Grab.interactionManager.CancelInteractableSelection((UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)kohlJar.Grab);
                kohlJar.gameObject.SetActive(false);
            }
            Game.NotifyStateChanged();
        }

        // ───────── name & engraving ─────────

        void OpenKeyboard()
        {
            if (!m_Keyboard) return;
            m_Keyboard.Open(keyboardAnchor, Game.State.playerName, name =>
            {
                Game.State.SetName(name);
                Game.NotifyStateChanged();
            });
        }

        void StartEngraving()
        {
            PlaceShardOnBench();
            var shardId = GameState.BuildShardId(Game.State.playerId, Game.State.playerName);
            shard.Inscribe(Game.State.playerName, shardId, 0f);
            int letters = Mathf.Max(1, Game.Nabataean.Convert(Game.State.playerName).glyphCount);
            if (engraver) engraver.Begin(letters);
        }

        void OnEngraved()
        {
            if (Game.State.EngraveShard())
            {
                shard.Inscribe(Game.State.playerName, Game.State.shardId, 1f);
                Game.NotifyStateChanged();
            }
        }

        void PlaceShardOnBench()
        {
            if (!shard || !shardBenchPose) return;
            SetShardGrabbable(false);
            shard.transform.SetPositionAndRotation(shardBenchPose.position, shardBenchPose.rotation);
        }

        void SetShardGrabbable(bool grabbable)
        {
            if (!shard) return;
            var item = shard.Item;
            item.essential = grabbable;
            item.Grab.enabled = grabbable;
            var rb = shard.GetComponent<Rigidbody>();
            if (rb) rb.isKinematic = !grabbable;
        }

        public override bool CanResetPuzzle => IsCurrent(QuestStep.MarketEngrave) || IsCurrent(QuestStep.MarketEnterName);

        public override void ResetPuzzle()
        {
            if (IsCurrent(QuestStep.MarketEngrave)) StartEngraving();
            else if (IsCurrent(QuestStep.MarketEnterName)) OpenKeyboard();
            Game.Menus.ShowToast("toast.puzzle_reset");
        }

        /// <summary>Used by the assisted route at Qasr al-Farid when no name was entered yet.</summary>
        /// <summary>Makes the engraved shard grabbable and puts it in the satchel (assisted route at Qasr al-Farid).</summary>
        public void HandOverShard()
        {
            if (!shard) return;
            shard.Inscribe(Game.State.playerName, Game.State.shardId, 1f);
            SetShardGrabbable(true);
            if (!shard.Item.IsHeldByHand && Game.SatchelSocket) Game.SatchelSocket.ForceInsert(shard.Item);
        }

        public void OpenKeyboardAt(Transform anchor, System.Action<string> onConfirm)
        {
            if (m_Keyboard) m_Keyboard.Open(anchor, Game.State.playerName, onConfirm);
        }
    }
}
