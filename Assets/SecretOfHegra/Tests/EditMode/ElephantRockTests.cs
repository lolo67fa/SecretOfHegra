using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace SecretOfHegra.Tests
{
    /// <summary>Sign 1 on the Elephant Rock: sequence rules, save compatibility and progression gates.</summary>
    public class ElephantRockTests
    {
        const string StringsPath = "Assets/SecretOfHegra/Data/ui_strings.json";
        const string DialoguePath = "Assets/SecretOfHegra/Data/dialogue.json";
        const string TheaterStopPath = "Assets/SecretOfHegra/Scripts/Runtime/Stops/TheaterStop.cs";

        [Test]
        public void Sequence_InOrder_Solves()
        {
            var seq = new GlyphSequence(1, 4, 3);
            Assert.AreEqual(GlyphSequence.Result.Progress, seq.Select(1));
            Assert.AreEqual(GlyphSequence.Result.Progress, seq.Select(4));
            Assert.AreEqual(GlyphSequence.Result.Solved, seq.Select(3));
            Assert.IsTrue(seq.Solved);
            Assert.AreEqual(GlyphSequence.Result.Ignored, seq.Select(1), "picks after solving change nothing");
        }

        [Test]
        public void Sequence_WrongPick_RestartsAttempt_AndCountsMistake()
        {
            var seq = new GlyphSequence(1, 4, 3);
            seq.Select(1);
            Assert.AreEqual(GlyphSequence.Result.Wrong, seq.Select(3));
            Assert.AreEqual(0, seq.Progress);
            Assert.AreEqual(1, seq.Mistakes);
            Assert.AreEqual(1, seq.Expected);
            // a fresh attempt still works
            seq.Select(1); seq.Select(4);
            Assert.AreEqual(GlyphSequence.Result.Solved, seq.Select(3));
        }

        [Test]
        public void RevealingLetters_DoesNotCompleteTheStep()
        {
            var s = GameState.CreateNew(false);
            s.step = QuestStep.TheaterFindInscription;
            s.rockGlyphsRevealed = true;
            Assert.IsFalse(QuestFlow.TryComplete(s, QuestStep.TheaterFindInscription));
            Assert.IsTrue(s.GrantSign(SignId.Inscription));
            Assert.IsFalse(s.GrantSign(SignId.Inscription), "Sign 1 is never granted twice");
            Assert.AreEqual(1, QuestFlow.AutoAdvance(s));
            Assert.AreEqual(QuestStep.IthlibIntro, s.step);
        }

        [Test]
        public void OldSave_WithSignOne_KeepsIt()
        {
            // a save written before the Elephant Rock existed (no rockGlyphsRevealed field)
            var old = GameState.CreateNew(false);
            old.step = QuestStep.IthlibPuzzle;
            old.GrantSign(SignId.Inscription);
            var json = Regex.Replace(JsonUtility.ToJson(old, true), @"\s*""rockGlyphsRevealed"":\s*(true|false),?", "");
            StringAssert.DoesNotContain("rockGlyphsRevealed", json);

            var dir = Path.Combine(Path.GetTempPath(), "hegra_test_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                File.WriteAllText(Path.Combine(dir, SaveSystem.ProgressFile), json);
                var loaded = new SaveSystem(dir).LoadProgress();
                Assert.IsNotNull(loaded);
                Assert.IsTrue(loaded.signInscription);
                Assert.IsFalse(loaded.rockGlyphsRevealed);
                Assert.AreEqual(QuestStep.IthlibPuzzle, loaded.step);
            }
            finally { Directory.Delete(dir, true); }
        }

        [Test]
        public void RevealedLetters_SurviveSaveAndLoad()
        {
            var dir = Path.Combine(Path.GetTempPath(), "hegra_test_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var saves = new SaveSystem(dir);
                var s = GameState.CreateNew(false);
                s.step = QuestStep.TheaterFindInscription;
                s.rockGlyphsRevealed = true;
                saves.SaveProgress(s);
                var loaded = saves.LoadProgress();
                Assert.IsTrue(loaded.rockGlyphsRevealed);
                Assert.IsFalse(loaded.signInscription);
            }
            finally { Directory.Delete(dir, true); }
        }

        [Test]
        public void FinalDoor_StillNeedsShardAndThreeSigns()
        {
            var s = GameState.CreateNew(false);
            s.SetName("Lulu");
            s.EngraveShard();
            s.shardInserted = true;
            s.GrantSign(SignId.Water);
            s.GrantSign(SignId.Eagle);
            Assert.IsFalse(s.CanOpenFinalDoor(), "missing Sign 1");
            s.GrantSign(SignId.Inscription);
            Assert.IsTrue(s.CanOpenFinalDoor());
        }

        [Test]
        public void RockTexts_ExistInBothLanguages()
        {
            Loc.Clear();
            Loc.LoadTable(File.ReadAllText(StringsPath));
            Loc.LoadTable(File.ReadAllText(DialoguePath)); // the dialogue file carries its line text in "entries"
            foreach (var key in new[]
            {
                "obj.TheaterFindInscription", "ctl.TheaterFindInscription", "obj.rock_repeat", "ctl.rock_repeat", "ui.replay_glyphs",
                "toast.rock_approach", "toast.rock_wrong", "toast.rock_hint", "edu.elephant.title", "edu.elephant.body",
                "hint.TheaterFindInscription.1", "hint.TheaterFindInscription.2", "hint.TheaterFindInscription.3", "rock_revealed_01"
            })
            {
                Assert.IsTrue(Loc.Has(key), key);
                Assert.IsFalse(string.IsNullOrWhiteSpace(Loc.Get(key, Language.Arabic)), key + " (ar)");
                Assert.IsFalse(string.IsNullOrWhiteSpace(Loc.Get(key, Language.English)), key + " (en)");
            }
            foreach (var key in new[] { "obj.TheaterFindInscription", "hint.TheaterFindInscription.1", "theater_after_01", "theater_play_10" })
                StringAssert.DoesNotContain("old wall", Loc.Get(key, Language.English), key + " still points at the old wall");
        }

        [Test]
        public void TheaterStop_GrantsSignOneOnlyWhenTheRockIsSolved()
        {
            var src = File.ReadAllText(TheaterStopPath);
            int grants = Regex.Matches(src, @"GrantSign\(SignId\.Inscription\)").Count;
            // one grant in OnRockSolved, one legacy fallback used only when no rock puzzle is assigned
            Assert.AreEqual(2, grants);
            StringAssert.Contains("if (!rock)", src);
        }
    }
}
