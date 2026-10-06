using System;
using System.Collections.Generic;

namespace SecretOfHegra
{
    /// <summary>
    /// The explicit progression state machine. Each step declares its stop, how it completes, the
    /// state condition that must hold before it may advance, and the next step. Pure C# so the whole
    /// route can be exercised by edit-mode tests.
    /// </summary>
    public static class QuestFlow
    {
        public sealed class StepInfo
        {
            public QuestStep step;
            public StopId stop;
            public StepKind kind;
            public Func<GameState, bool> condition;
            public string conditionText;
        }

        static readonly List<StepInfo> s_Steps = new List<StepInfo>();
        static readonly Dictionary<QuestStep, int> s_Index = new Dictionary<QuestStep, int>();

        static QuestFlow()
        {
            Add(QuestStep.CampWelcome, StopId.Camp, StepKind.Event, null, "Shuʿayd's welcome finished");
            Add(QuestStep.CampGrabLamp, StopId.Camp, StepKind.Condition, s => s.lampAcquired, "Lamp grabbed");
            Add(QuestStep.CampTeleport, StopId.Camp, StepKind.Condition, s => s.tutorialTeleported, "Player teleported");
            Add(QuestStep.CampSnapTurn, StopId.Camp, StepKind.Condition, s => s.tutorialTurned, "Player snap-turned");
            Add(QuestStep.CampStoreLamp, StopId.Camp, StepKind.Condition, s => s.lampStoredOnce, "Lamp stored on belt");
            Add(QuestStep.CampFarewell, StopId.Camp, StepKind.Event, s => s.coinsReceived, "Coin pouch received and farewell finished");

            Add(QuestStep.MarketMeetKamkam, StopId.Market, StepKind.Event, null, "Player reached Kamkam");
            Add(QuestStep.MarketLegend, StopId.Market, StepKind.Event, null, "Legend told");
            Add(QuestStep.MarketBuyKohl, StopId.Market, StepKind.Condition, s => s.hasKohl, "Kohl bought");
            Add(QuestStep.MarketApplyKohl, StopId.Market, StepKind.Condition, s => s.kohlApplied, "Kohl applied");
            Add(QuestStep.MarketMeetWahballahi, StopId.Market, StepKind.Event, null, "Carving lesson heard");
            Add(QuestStep.MarketEnterName, StopId.Market, StepKind.Condition, s => s.HasName, "Name entered");
            Add(QuestStep.MarketEngrave, StopId.Market, StepKind.Condition, s => s.shardEngraved, "Shard engraved");
            Add(QuestStep.MarketReceiveShard, StopId.Market, StepKind.Event, s => s.shardEngraved, "Shard handed over");

            Add(QuestStep.TheaterIntro, StopId.Theater, StepKind.Event, null, "Maliku's introduction finished");
            Add(QuestStep.TheaterPerformance, StopId.Theater, StepKind.Event, null, "Performance finished or skipped");
            Add(QuestStep.TheaterFindInscription, StopId.Theater, StepKind.Condition, s => s.signInscription, "Sign 1 (Inscription) granted");

            Add(QuestStep.IthlibIntro, StopId.Ithlib, StepKind.Event, null, "Arrived at Jabal Ithlib");
            Add(QuestStep.IthlibPuzzle, StopId.Ithlib, StepKind.Condition, s => s.signWater, "Sign 2 (Water) granted");

            Add(QuestStep.BintIntro, StopId.Bint, StepKind.Event, null, "Arrived at Qasr al-Bint");
            Add(QuestStep.BintAlign, StopId.Bint, StepKind.Condition, s => s.signEagle, "Sign 3 (Eagle) granted");

            Add(QuestStep.FaridIntro, StopId.Farid, StepKind.Event, null, "Arrived at Qasr al-Farid");
            Add(QuestStep.FaridInsertShard, StopId.Farid, StepKind.Condition, s => s.CanOpenFinalDoor(), "Own shard inserted with all signs (or assisted route)");
            Add(QuestStep.FaridDoorOpening, StopId.Farid, StepKind.Event, s => s.doorOpened, "Door finished opening");
            Add(QuestStep.FaridOpenChest, StopId.Farid, StepKind.Condition, s => s.chestOpened, "Chest opened");
            Add(QuestStep.FaridFinale, StopId.Farid, StepKind.Event, null, "Final dialogue finished");
            Add(QuestStep.Complete, StopId.Farid, StepKind.Event, s => false, "Journey complete");
        }

        static void Add(QuestStep step, StopId stop, StepKind kind, Func<GameState, bool> condition, string text)
        {
            s_Index[step] = s_Steps.Count;
            s_Steps.Add(new StepInfo { step = step, stop = stop, kind = kind, condition = condition, conditionText = text });
        }

        public static IReadOnlyList<StepInfo> Steps => s_Steps;
        public static QuestStep FirstStep => QuestStep.CampWelcome;

        public static StepInfo Info(QuestStep step)
        {
            return s_Index.TryGetValue(step, out var i) ? s_Steps[i] : null;
        }

        public static QuestStep Next(QuestStep step)
        {
            if (step == QuestStep.None)
                return FirstStep;
            if (!s_Index.TryGetValue(step, out var i) || i + 1 >= s_Steps.Count)
                return QuestStep.Complete;
            return s_Steps[i + 1].step;
        }

        public static StopId StopOf(QuestStep step)
        {
            if (step == QuestStep.None)
                return StopId.Camp;
            var info = Info(step);
            return info != null ? info.stop : StopId.Farid;
        }

        /// <summary>First step of a stop — used as the checkpoint when continuing a save.</summary>
        public static QuestStep FirstStepOf(StopId stop)
        {
            foreach (var s in s_Steps)
                if (s.stop == stop)
                    return s.step;
            return QuestStep.CampWelcome;
        }

        public static bool IsSatisfied(QuestStep step, GameState state)
        {
            var info = Info(step);
            if (info == null)
                return false;
            return info.condition == null || info.condition(state);
        }

        /// <summary>
        /// Tries to complete <paramref name="step"/>. Only the current step can complete, and only when its
        /// condition holds, which prevents repeated or out-of-order completion. Returns true on advance.
        /// </summary>
        public static bool TryComplete(GameState state, QuestStep step)
        {
            if (state.step != step || step == QuestStep.Complete)
                return false;
            if (!IsSatisfied(step, state))
                return false;
            state.step = Next(step);
            state.hintLevel = 0;
            return true;
        }

        /// <summary>
        /// Advances through consecutive condition steps that are already satisfied (e.g. the inscription was
        /// found during the performance). Returns how many steps were advanced.
        /// </summary>
        public static int AutoAdvance(GameState state)
        {
            int advanced = 0;
            while (true)
            {
                var info = Info(state.step);
                if (info == null || info.kind != StepKind.Condition || !IsSatisfied(state.step, state))
                    return advanced;
                state.step = Next(state.step);
                state.hintLevel = 0;
                advanced++;
            }
        }

        /// <summary>Applies the tutorial skip: grants the camp prerequisites and jumps to the farewell.</summary>
        public static bool SkipTutorial(GameState state)
        {
            if (state.step < QuestStep.CampWelcome || state.step >= QuestStep.CampFarewell)
                return false;
            state.lampAcquired = true;
            state.tutorialTeleported = true;
            state.tutorialTurned = true;
            state.lampStoredOnce = true;
            state.tutorialSkipped = true;
            state.step = QuestStep.CampFarewell;
            state.hintLevel = 0;
            return true;
        }

        /// <summary>Should Kamkam offer the assisted route because the player is running late?</summary>
        public static bool ShouldOfferAssist(GameState state, float lateThresholdSeconds, float reofferIntervalSeconds)
        {
            if (state.museumMode || state.assistedRoute || state.step >= QuestStep.FaridInsertShard || state.step == QuestStep.None)
                return false;
            float threshold = lateThresholdSeconds + state.assistOffersDeclined * reofferIntervalSeconds;
            return state.elapsedSeconds >= threshold;
        }
    }
}
