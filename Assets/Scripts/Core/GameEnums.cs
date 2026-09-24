namespace SlotGame.Core
{
    /// <summary>
    /// Identifies the distinct symbol types appearing on the slot reels.
    /// </summary>
    public enum SymbolType
    {
        Seven = 0,    // Red Lucky Seven (Jackpot tier)
        Cherry = 1,   // Classic Cherry
        Bell = 2,     // Golden Bell (High tier)
        Bar = 3,      // Triple BAR (Medium tier)
        Bonus = 4     // Free Spins / Scatter Bonus trigger
    }

    /// <summary>
    /// Lifecycle states for the main slot machine state machine.
    /// </summary>
    public enum SlotMachineState
    {
        Idle,               // Awaiting player spin input
        LeverPulling,       // Lever animation in progress
        Spinning,           // All reels actively rotating at sustained speed
        Stopping,           // Reels stopping in staggered sequence (1 -> 2 -> 3)
        Evaluating,         // Win evaluation and payline calculation
        WinCelebration,     // Particle and audio celebration sequence
        BonusGame           // Free spins or special mini-game active
    }

    /// <summary>
    /// Payout tier classification for win effects and celebrations.
    /// </summary>
    public enum WinTier
    {
        None,
        Small,        // 2x - 5x bet
        Medium,       // 10x - 20x bet
        Large,        // 25x - 40x bet
        Jackpot       // 50x+ bet (3x Lucky Sevens)
    }
}
