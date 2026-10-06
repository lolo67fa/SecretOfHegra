namespace SecretOfHegra
{
    /// <summary>
    /// Every step of the six-stop journey, in play order. Numeric values are stable because they
    /// are written to save files; never renumber existing entries.
    /// </summary>
    public enum QuestStep
    {
        None = 0,

        // Stop 1 — Caravan Camp
        CampWelcome = 10,
        CampGrabLamp = 11,
        CampTeleport = 12,
        CampSnapTurn = 13,
        CampStoreLamp = 14,
        CampFarewell = 15,

        // Stop 2 — Market
        MarketMeetKamkam = 20,
        MarketLegend = 21,
        MarketBuyKohl = 22,
        MarketApplyKohl = 23,
        MarketMeetWahballahi = 24,
        MarketEnterName = 25,
        MarketEngrave = 26,
        MarketReceiveShard = 27,

        // Stop 3 — Theater Square
        TheaterIntro = 30,
        TheaterPerformance = 31,
        TheaterFindInscription = 32,

        // Stop 4 — Jabal Ithlib
        IthlibIntro = 40,
        IthlibPuzzle = 41,

        // Stop 5 — Qasr al-Bint
        BintIntro = 50,
        BintAlign = 51,

        // Stop 6 — Qasr al-Farid
        FaridIntro = 60,
        FaridInsertShard = 61,
        FaridDoorOpening = 62,
        FaridOpenChest = 63,
        FaridFinale = 64,

        Complete = 100
    }

    /// <summary>The six physical stops of the journey.</summary>
    public enum StopId
    {
        Camp = 0,
        Market = 1,
        Theater = 2,
        Ithlib = 3,
        Bint = 4,
        Farid = 5
    }

    /// <summary>The three signs required by the final door.</summary>
    public enum SignId
    {
        Inscription = 0,
        Water = 1,
        Eagle = 2
    }

    /// <summary>
    /// How a step finishes. Condition steps advance automatically as soon as their state condition is
    /// true; event steps advance when the owning stop reports the event (dialogue ended, door finished…).
    /// </summary>
    public enum StepKind
    {
        Event,
        Condition
    }
}
