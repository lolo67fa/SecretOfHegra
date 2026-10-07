using System.IO;
using NUnit.Framework;

namespace SecretOfHegra.Tests
{
    public class ProgressionTests
    {
        static GameState NewState()
        {
            var s = GameState.CreateNew(false);
            s.step = QuestFlow.FirstStep;
            return s;
        }

        /// <summary>Plays the normal route by satisfying each step's condition the way the stops do.</summary>
        static void Satisfy(GameState s, QuestStep step)
        {
            switch (step)
            {
                case QuestStep.CampGrabLamp: s.lampAcquired = true; break;
                case QuestStep.CampTeleport: s.tutorialTeleported = true; break;
                case QuestStep.CampSnapTurn: s.tutorialTurned = true; break;
                case QuestStep.CampStoreLamp: s.lampStoredOnce = true; break;
                case QuestStep.CampFarewell: s.ReceiveStartingCoins(); break;
                case QuestStep.MarketBuyKohl: s.BuyKohl(); break;
                case QuestStep.MarketApplyKohl: s.ApplyKohl(); break;
                case QuestStep.MarketEnterName: s.SetName("Lulu"); break;
                case QuestStep.MarketEngrave: s.EngraveShard(); break;
                case QuestStep.TheaterFindInscription: s.GrantSign(SignId.Inscription); break;
                case QuestStep.IthlibPuzzle: s.GrantSign(SignId.Water); break;
                case QuestStep.BintAlign: s.GrantSign(SignId.Eagle); break;
                case QuestStep.FaridInsertShard: s.shardInserted = true; break;
                case QuestStep.FaridDoorOpening: s.doorOpened = true; break;
                case QuestStep.FaridOpenChest: s.chestOpened = true; break;
            }
        }

        [Test]
        public void NormalRoute_ReachesComplete()
        {
            var s = NewState();
            int guard = 0;
            while (s.step != QuestStep.Complete && guard++ < 100)
            {
                var step = s.step;
                Satisfy(s, step);
                if (QuestFlow.Info(step).kind == StepKind.Condition) QuestFlow.AutoAdvance(s);
                else Assert.IsTrue(QuestFlow.TryComplete(s, step), $"Could not complete {step}");
                QuestFlow.AutoAdvance(s);
            }
            Assert.AreEqual(QuestStep.Complete, s.step);
            Assert.IsTrue(s.HasAllSigns);
            Assert.AreEqual(GameState.StartingCoins - GameState.KohlPrice, s.coins);
        }

        [Test]
        public void ConditionStep_DoesNotAdvanceWithoutCondition()
        {
            var s = NewState();
            Assert.IsTrue(QuestFlow.TryComplete(s, QuestStep.CampWelcome));
            Assert.AreEqual(QuestStep.CampGrabLamp, s.step);
            Assert.IsFalse(QuestFlow.TryComplete(s, QuestStep.CampGrabLamp));
            Assert.AreEqual(0, QuestFlow.AutoAdvance(s));
        }

        [Test]
        public void OnlyCurrentStepCanComplete()
        {
            var s = NewState();
            Assert.IsFalse(QuestFlow.TryComplete(s, QuestStep.MarketLegend));
            Assert.IsFalse(QuestFlow.TryComplete(s, QuestStep.FaridFinale));
            Assert.AreEqual(QuestFlow.FirstStep, s.step);
        }

        [Test]
        public void Signs_AreGrantedExactlyOnce()
        {
            var s = NewState();
            Assert.IsTrue(s.GrantSign(SignId.Water));
            Assert.IsFalse(s.GrantSign(SignId.Water));
            Assert.AreEqual(1, s.SignCount);
        }

        [Test]
        public void StartingCoins_GivenOnce_AndKohlNeverBlocks()
        {
            var s = NewState();
            Assert.IsTrue(s.ReceiveStartingCoins());
            Assert.IsFalse(s.ReceiveStartingCoins());
            Assert.AreEqual(GameState.StartingCoins, s.coins);
            s.coins = 0;
            Assert.IsTrue(s.BuyKohl(), "Kamkam gives kohl as a gift when the pouch is empty");
            Assert.IsFalse(s.BuyKohl());
        }

        [Test]
        public void FinalDoor_RequiresOwnShardAndAllSigns()
        {
            var s = NewState();
            s.SetName("لولو");
            Assert.IsTrue(s.EngraveShard());
            s.shardInserted = true;
            Assert.IsFalse(s.CanOpenFinalDoor());
            s.GrantSign(SignId.Inscription);
            s.GrantSign(SignId.Water);
            Assert.IsFalse(s.CanOpenFinalDoor());
            s.GrantSign(SignId.Eagle);
            Assert.IsTrue(s.CanOpenFinalDoor());
            Assert.IsTrue(s.ShardBelongsToPlayer(s.shardId));
            Assert.IsFalse(s.ShardBelongsToPlayer(GameState.BuildShardId("someoneelse", "لولو")));
        }

        [Test]
        public void ShardId_IsStableForSamePlayerAndName()
        {
            Assert.AreEqual(GameState.BuildShardId("abc", "Lulu"), GameState.BuildShardId("abc", "Lulu"));
            Assert.AreNotEqual(GameState.BuildShardId("abc", "Lulu"), GameState.BuildShardId("abc", "Lula"));
        }

        [TestCase(QuestStep.CampGrabLamp)]
        [TestCase(QuestStep.MarketLegend)]
        [TestCase(QuestStep.TheaterPerformance)]
        [TestCase(QuestStep.IthlibPuzzle)]
        [TestCase(QuestStep.BintAlign)]
        public void AssistedRoute_FromAnyStep_ReachesEndingWithoutSoftLock(QuestStep from)
        {
            var s = NewState();
            s.step = from;
            bool needsName = s.ApplyAssistedRoute();
            Assert.IsTrue(needsName, "No name was entered yet, so the assisted route must ask for one");
            Assert.AreEqual(QuestStep.FaridInsertShard, s.step);
            s.SetName("Lulu");
            Assert.IsTrue(s.EngraveShard());
            s.shardInserted = true;
            Assert.IsTrue(s.CanOpenFinalDoor());
            Assert.AreEqual(1, QuestFlow.AutoAdvance(s));
            Assert.AreEqual(QuestStep.FaridDoorOpening, s.step);
            s.doorOpened = true;
            Assert.IsTrue(QuestFlow.TryComplete(s, QuestStep.FaridDoorOpening));
            s.chestOpened = true;
            QuestFlow.AutoAdvance(s);
            Assert.IsTrue(QuestFlow.TryComplete(s, QuestStep.FaridFinale));
            Assert.AreEqual(QuestStep.Complete, s.step);
        }

        [Test]
        public void AssistedRoute_DoesNotDuplicateExistingSigns()
        {
            var s = NewState();
            s.GrantSign(SignId.Inscription);
            s.ApplyAssistedRoute();
            Assert.IsFalse(s.signInscriptionByAssist);
            Assert.IsTrue(s.signWaterByAssist && s.signEagleByAssist);
            Assert.AreEqual(3, s.SignCount);
        }

        [Test]
        public void LateAssist_OfferedOnlyWhenLate_AndNotInMuseumMode()
        {
            var s = NewState();
            s.elapsedSeconds = 100f;
            Assert.IsFalse(QuestFlow.ShouldOfferAssist(s, 750f, 120f));
            s.elapsedSeconds = 800f;
            Assert.IsTrue(QuestFlow.ShouldOfferAssist(s, 750f, 120f));
            s.assistOffersDeclined = 1;
            Assert.IsFalse(QuestFlow.ShouldOfferAssist(s, 750f, 120f));
            s.museumMode = true;
            s.elapsedSeconds = 5000f;
            Assert.IsFalse(QuestFlow.ShouldOfferAssist(s, 750f, 120f));
        }

        [Test]
        public void SkipTutorial_GrantsLessonsAndJumpsToFarewell()
        {
            var s = NewState();
            Assert.IsTrue(QuestFlow.SkipTutorial(s));
            Assert.AreEqual(QuestStep.CampFarewell, s.step);
            Assert.IsTrue(s.lampAcquired && s.lampStoredOnce && s.tutorialSkipped);
            Assert.IsFalse(QuestFlow.SkipTutorial(s));
        }

        [Test]
        public void SaveAndLoad_RoundTrip()
        {
            var dir = Path.Combine(Path.GetTempPath(), "hegra_test_" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var saves = new SaveSystem(dir);
                Assert.IsFalse(saves.HasProgress());
                var s = NewState();
                s.SetName("لولو");
                s.EngraveShard();
                s.GrantSign(SignId.Eagle);
                s.step = QuestStep.BintAlign;
                s.elapsedSeconds = 321f;
                saves.SaveProgress(s);
                var loaded = saves.LoadProgress();
                Assert.IsNotNull(loaded);
                Assert.AreEqual(s.playerId, loaded.playerId);
                Assert.AreEqual(QuestStep.BintAlign, loaded.step);
                Assert.AreEqual("لولو", loaded.playerName);
                Assert.AreEqual(s.shardId, loaded.shardId);
                Assert.IsTrue(loaded.signEagle);
                Assert.AreEqual(321f, loaded.elapsedSeconds, 0.01f);
                saves.DeleteProgress();
                Assert.IsFalse(saves.HasProgress());

                var settings = new SettingsData { language = Language.English, subtitleScale = 9f, snapTurnAngle = 33f };
                saves.SaveSettings(settings);
                var ls = saves.LoadSettings();
                Assert.AreEqual(Language.English, ls.language);
                Assert.AreEqual(SettingsData.MaxSubtitleScale, ls.subtitleScale, 0.001f);
                Assert.AreEqual(45f, ls.snapTurnAngle);
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }

        [Test]
        public void CorruptSave_IsIgnored()
        {
            var dir = Path.Combine(Path.GetTempPath(), "hegra_test_" + System.Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, SaveSystem.ProgressFile), "{ not json");
                Assert.IsNull(new SaveSystem(dir).LoadProgress());
            }
            finally { Directory.Delete(dir, true); }
        }

        [Test]
        public void WalkSpeed_PresetsAreComfortable_AndSprintStaysBelowFourMetres()
        {
            var s = new SettingsData();
            Assert.AreEqual(MoveSpeed.Normal, s.moveSpeed);
            Assert.IsTrue(s.smoothMovement);
            Assert.That(s.WalkSpeed, Is.InRange(1.2f, 1.6f));
            s.moveSpeed = MoveSpeed.Slow;
            Assert.Less(s.WalkSpeed, 1.2f);
            s.moveSpeed = MoveSpeed.Fast;
            Assert.Greater(s.WalkSpeed, 1.6f);
            Assert.LessOrEqual(System.Math.Min(4f, s.WalkSpeed * SettingsData.SprintMultiplier), 4f);
        }

        [Test]
        public void MoveSettings_AreClampedAndPersisted()
        {
            var dir = Path.Combine(Path.GetTempPath(), "hegra_test_" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var saves = new SaveSystem(dir);
                saves.SaveSettings(new SettingsData { moveSpeed = MoveSpeed.Fast, smoothTurn = true, smoothMovement = false });
                var ls = saves.LoadSettings();
                Assert.AreEqual(MoveSpeed.Fast, ls.moveSpeed);
                Assert.IsTrue(ls.smoothTurn);
                Assert.IsFalse(ls.smoothMovement);

                var bad = new SettingsData { moveSpeed = (MoveSpeed)7 };
                bad.Clamp();
                Assert.AreEqual(MoveSpeed.Normal, bad.moveSpeed);
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }
    }

    public class TextTests
    {
        [Test]
        public void Shaper_UsesContextualForms_AndLamAlef()
        {
            // "لا" becomes one isolated lam-alef ligature
            Assert.AreEqual("ﻻ", ArabicShaper.Shape("لا"));
            // "بب": initial beh + final beh
            Assert.AreEqual("ﺑﺐ", ArabicShaper.Shape("بب"));
            // non-joining alef breaks the word: "اب" = isolated alef + isolated beh
            Assert.AreEqual("ﺍﺏ", ArabicShaper.Shape("اب"));
        }

        [Test]
        public void Shaper_KeepsLatinRunsReadable()
        {
            // TMP RTL reverses glyph order, so Latin runs are pre-reversed
            var s = ArabicShaper.ShapeForRtl("اسم Lulu");
            StringAssert.Contains("uluL", s);
        }

        [Test]
        public void Nabataean_MapsNameAndMarksMissingLetters()
        {
            var json = File.ReadAllText("Assets/SecretOfHegra/Data/nabataean_map.json");
            var conv = NabataeanConverter.FromJson(json);
            var lulu = conv.Convert("Lulu");
            Assert.AreEqual(4, lulu.glyphCount); // L W L W (final forms on last letter)
            Assert.AreEqual(0, lulu.missingCount);
            var ar = conv.Convert("لولو");
            Assert.AreEqual(lulu.text, ar.text, "Arabic and English presets give the same letters");
            var weird = conv.Convert("Lu#");
            Assert.AreEqual(1, weird.missingCount, "Unknown characters become a neutral dot, never a broken glyph");
            conv.hasGlyph = cp => false;
            Assert.AreEqual(conv.Convert("Lulu").glyphCount, conv.Convert("Lulu").missingCount);
        }
    }

    public class WaterPuzzleTests
    {
        [Test]
        public void InitialLayout_IsNotSolved_AndSpillsIntoBasin()
        {
            var n = WaterNetwork.CreateIthlib();
            var f = n.Flow();
            Assert.IsFalse(f.solved);
            Assert.IsTrue(f.wet.Contains((2, 1)), "wrong first turn leads to the dry spill basin");
        }

        [Test]
        public void SolvedRotations_ReachCistern()
        {
            var n = WaterNetwork.CreateIthlib();
            n[1, 0].rotation = 3; n[0, 2].rotation = 0; n[1, 2].rotation = 1;
            Assert.IsTrue(n.Flow().solved);
            Assert.IsTrue(n.AllGatesSolved());
        }

        [Test]
        public void StraightGate_HalfTurnIsEquivalent()
        {
            var n = WaterNetwork.CreateIthlib();
            n[1, 0].rotation = 3; n[0, 2].rotation = 0; n[1, 2].rotation = 3;
            Assert.IsTrue(n.Flow().solved);
        }

        [Test]
        public void OneWrongGate_BlocksTheCistern()
        {
            var n = WaterNetwork.CreateIthlib();
            n[1, 0].rotation = 3; n[0, 2].rotation = 1; n[1, 2].rotation = 1;
            Assert.IsFalse(n.Flow().solved);
        }

        [Test]
        public void RotateMask_IsClockwise()
        {
            Assert.AreEqual(WaterNetwork.E, WaterNetwork.Rotate(WaterNetwork.N, 1));
            Assert.AreEqual(WaterNetwork.W | WaterNetwork.N, WaterNetwork.Rotate(WaterNetwork.N | WaterNetwork.E, 3));
        }
    }
}
