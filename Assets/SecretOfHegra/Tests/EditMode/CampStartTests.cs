using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace SecretOfHegra.Tests
{
    /// <summary>Journey start: the lamp starts in the player's hand, Shuʿayd only hands over the coins.</summary>
    public class CampStartTests
    {
        [Test]
        public void TakeCoinsStep_KeepsTheOldSaveNumber()
        {
            // saves store the step as a number; 11 used to be "grab the lamp"
            Assert.AreEqual(11, (int)QuestStep.CampTakeCoins);
            Assert.AreEqual(QuestStep.CampTakeCoins, QuestFlow.Next(QuestStep.CampWelcome));
            Assert.AreEqual(QuestStep.CampTeleport, QuestFlow.Next(QuestStep.CampTakeCoins));
        }

        [Test]
        public void TakeCoinsStep_NeedsTheCoins_NotTheLamp()
        {
            var s = GameState.CreateNew(false);
            s.step = QuestStep.CampTakeCoins;
            s.lampAcquired = true;
            Assert.IsFalse(QuestFlow.TryComplete(s, QuestStep.CampTakeCoins));
            Assert.IsTrue(s.ReceiveStartingCoins());
            Assert.AreEqual(GameState.StartingCoins, s.coins);
            Assert.AreEqual(1, QuestFlow.AutoAdvance(s));
            Assert.AreEqual(QuestStep.CampTeleport, s.step);
            Assert.IsFalse(s.ReceiveStartingCoins(), "the coins are given only once (the farewell does not add more)");
        }

        [Test]
        public void CoinsTakenDuringWelcome_SkipStraightToTeleport()
        {
            var s = GameState.CreateNew(false);
            s.step = QuestStep.CampWelcome;
            s.ReceiveStartingCoins();
            Assert.IsTrue(QuestFlow.TryComplete(s, QuestStep.CampWelcome));
            QuestFlow.AutoAdvance(s);
            Assert.AreEqual(QuestStep.CampTeleport, s.step);
        }

        [Test]
        public void OldSave_AtFormerGrabLampStep_LoadsAsTakeCoins()
        {
            var old = GameState.CreateNew(false);
            old.step = QuestStep.CampTakeCoins;
            old.lampAcquired = false;   // a save made before the lamp started in the hand
            var dir = Path.Combine(Path.GetTempPath(), "hegra_test_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var json = JsonUtility.ToJson(old, true);
                StringAssert.Contains("\"step\": 11", json);   // the step is saved as its number
                File.WriteAllText(Path.Combine(dir, SaveSystem.ProgressFile), json);
                var loaded = new SaveSystem(dir).LoadProgress();
                Assert.IsNotNull(loaded);
                Assert.AreEqual(QuestStep.CampTakeCoins, loaded.step);
                Assert.IsNotNull(QuestFlow.Info(loaded.step));
                Assert.IsFalse(loaded.coinsReceived);
            }
            finally { Directory.Delete(dir, true); }
        }
    }
}
