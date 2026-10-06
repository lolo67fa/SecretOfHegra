using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace SecretOfHegra.Tests
{
    public class ChatterTests
    {
        const string StringsPath = "Assets/SecretOfHegra/Data/ui_strings.json";
        const string MarketBuilderPath = "Assets/SecretOfHegra/Scripts/Editor/HegraBuilder.Market.cs";

        // steps where MarketChatter stays silent, so a hint for them would never be heard
        static readonly QuestStep[] s_Quiet = { QuestStep.MarketLegend, QuestStep.MarketBuyKohl, QuestStep.MarketApplyKohl };

        [Serializable] class Table { public Loc.Entry[] entries; }

        static List<Loc.Entry> ChatterEntries()
        {
            var table = JsonUtility.FromJson<Table>(File.ReadAllText(StringsPath));
            var list = new List<Loc.Entry>();
            foreach (var e in table.entries)
                if (e.key != null && e.key.StartsWith("chatter.")) list.Add(e);
            return list;
        }

        [Test]
        public void ChatterLines_HaveArabicAndEnglish_AndUniqueKeys()
        {
            var entries = ChatterEntries();
            Assert.IsNotEmpty(entries);
            var seen = new HashSet<string>();
            foreach (var e in entries)
            {
                Assert.IsTrue(seen.Add(e.key), $"Duplicate key {e.key}");
                Assert.IsFalse(string.IsNullOrWhiteSpace(e.ar), $"{e.key} has no Arabic");
                Assert.IsFalse(string.IsNullOrWhiteSpace(e.en), $"{e.key} has no English");
            }
        }

        [Test]
        public void ChatterLines_FitInABubble()
        {
            // Speak() caps a line at 6 s; longer text would vanish before it can be read
            foreach (var e in ChatterEntries())
            {
                Assert.LessOrEqual(e.ar.Length, 80, $"{e.key} Arabic is too long for a bubble");
                Assert.LessOrEqual(e.en.Length, 80, $"{e.key} English is too long for a bubble");
            }
        }

        [Test]
        public void HintKeys_NameARealStep_ThatIsNotQuiet()
        {
            foreach (var e in ChatterEntries())
            {
                if (!e.key.StartsWith("chatter.hint.")) continue;
                var name = e.key.Substring("chatter.hint.".Length);
                Assert.IsTrue(Enum.TryParse(name, out QuestStep step), $"{e.key} does not match a QuestStep");
                Assert.AreEqual(-1, Array.IndexOf(s_Quiet, step), $"{e.key} is for a step where chatter is muted");
            }
        }

        [Test]
        public void MarketBuilder_OnlyUsesExistingLineKeys()
        {
            Loc.Clear();
            Loc.LoadTable(File.ReadAllText(StringsPath));
            var source = File.ReadAllText(MarketBuilderPath);
            var used = Regex.Matches(source, "\"(chatter\\.[A-Za-z]+\\.[A-Za-z0-9]+)\"");
            Assert.Greater(used.Count, 0, "No chatter keys found in the market builder");
            foreach (Match m in used)
                Assert.IsTrue(Loc.Has(m.Groups[1].Value), $"Missing string {m.Groups[1].Value}");
            Loc.Clear();
        }
    }
}
