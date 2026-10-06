using System;

namespace SecretOfHegra
{
    /// <summary>
    /// Serializable progress of one playthrough. Pure data plus small rule helpers so it can be
    /// unit-tested without a scene. Saved as JSON by <see cref="SaveSystem"/>.
    /// </summary>
    [Serializable]
    public class GameState
    {
        public const int SaveVersion = 1;
        public const int StartingCoins = 3;
        public const int KohlPrice = 1;

        public int version = SaveVersion;
        public string playerId = "";
        public QuestStep step = QuestStep.None;

        // Camp tutorial
        public bool lampAcquired;
        public bool tutorialTeleported;
        public bool tutorialTurned;
        public bool lampStoredOnce;
        public bool tutorialSkipped;

        // Market
        public int coins;
        public bool coinsReceived;
        public bool hasKohl;
        public bool kohlApplied;
        public string playerName = "";
        public bool shardEngraved;
        public string shardId = "";

        // Signs
        public bool signInscription;
        public bool signWater;
        public bool signEagle;
        public bool signInscriptionByAssist;
        public bool signWaterByAssist;
        public bool signEagleByAssist;

        // Elephant Rock inscription (Sign 1): the lamp has uncovered its letters. Older saves lack the field
        // and load it as false; a save that already holds Sign 1 shows the rock solved.
        public bool rockGlyphsRevealed;

        // Theater / finale
        public bool performanceSeen;
        public bool shardInserted;
        public bool doorOpened;
        public bool chestOpened;
        public bool souvenirSaved;

        // Route
        public bool assistedRoute;
        public bool museumMode;
        public float elapsedSeconds;
        public int assistOffersDeclined;
        public int hintLevel;

        public static GameState CreateNew(bool museumMode)
        {
            return new GameState
            {
                playerId = Guid.NewGuid().ToString("N"),
                step = QuestStep.None,
                museumMode = museumMode
            };
        }

        public bool HasSign(SignId id)
        {
            switch (id)
            {
                case SignId.Inscription: return signInscription;
                case SignId.Water: return signWater;
                default: return signEagle;
            }
        }

        public int SignCount => (signInscription ? 1 : 0) + (signWater ? 1 : 0) + (signEagle ? 1 : 0);
        public bool HasAllSigns => SignCount == 3;
        public bool HasName => !string.IsNullOrWhiteSpace(playerName);

        /// <summary>Grants a sign. Returns false if it was already granted (no duplicate rewards).</summary>
        public bool GrantSign(SignId id, bool byAssist = false)
        {
            if (HasSign(id))
                return false;
            switch (id)
            {
                case SignId.Inscription: signInscription = true; signInscriptionByAssist = byAssist; break;
                case SignId.Water: signWater = true; signWaterByAssist = byAssist; break;
                default: signEagle = true; signEagleByAssist = byAssist; break;
            }
            return true;
        }

        /// <summary>Gives the starting pouch once. Returns false if already received.</summary>
        public bool ReceiveStartingCoins()
        {
            if (coinsReceived)
                return false;
            coinsReceived = true;
            coins += StartingCoins;
            return true;
        }

        /// <summary>
        /// Pays for kohl once (a second call never charges again). Kamkam gives it as a gift if the pouch
        /// cannot cover the price, so progression never blocks. Returns false if kohl was already bought.
        /// </summary>
        public bool BuyKohl(int price = KohlPrice)
        {
            if (hasKohl)
                return false;
            price = Math.Max(0, price);
            if (coins >= price)
                coins -= price;
            hasKohl = true;
            return true;
        }

        public bool CanAfford(int price) => coins >= Math.Max(0, price);

        public bool ApplyKohl()
        {
            if (kohlApplied)
                return false;
            hasKohl = true;
            kohlApplied = true;
            return true;
        }

        public void SetName(string name)
        {
            playerName = (name ?? "").Trim();
            if (playerName.Length > NameRules.MaxLength)
                playerName = playerName.Substring(0, NameRules.MaxLength);
        }

        /// <summary>Engraves the shard once and binds it to this player's stable identity.</summary>
        public bool EngraveShard()
        {
            if (shardEngraved || !HasName)
                return false;
            shardEngraved = true;
            shardId = BuildShardId(playerId, playerName);
            return true;
        }

        public static string BuildShardId(string playerId, string name)
        {
            unchecked
            {
                // FNV-1a over the name keeps the id stable across sessions and platforms.
                uint hash = 2166136261;
                foreach (char c in name ?? "")
                {
                    hash ^= c;
                    hash *= 16777619;
                }
                return playerId + ":" + hash.ToString("x8");
            }
        }

        public bool ShardBelongsToPlayer(string candidateShardId)
        {
            return shardEngraved && !string.IsNullOrEmpty(candidateShardId) && candidateShardId == shardId;
        }

        /// <summary>
        /// The final door rule. Normal route: own shard inserted and all three signs.
        /// Assisted route: own shard inserted is enough (missing signs are granted by Kamkam).
        /// </summary>
        public bool CanOpenFinalDoor()
        {
            if (!shardInserted || !shardEngraved)
                return false;
            return HasAllSigns || assistedRoute;
        }

        /// <summary>
        /// Applies Kamkam's assisted route: everything required for the finale is granted,
        /// the shard is engraved if a name exists, and the step jumps to the final door.
        /// Returns true when the shard still needs a name before it can be engraved.
        /// </summary>
        public bool ApplyAssistedRoute()
        {
            assistedRoute = true;
            lampAcquired = true;
            lampStoredOnce = true;
            if (!coinsReceived)
                ReceiveStartingCoins();
            GrantSign(SignId.Inscription, true);
            GrantSign(SignId.Water, true);
            GrantSign(SignId.Eagle, true);
            if (HasName)
                EngraveShard();
            if (step < QuestStep.FaridInsertShard)
                step = QuestStep.FaridInsertShard;
            return !shardEngraved;
        }

        public GameState Clone()
        {
            return (GameState)MemberwiseClone();
        }
    }

    public static class NameRules
    {
        public const int MaxLength = 12;
    }
}
