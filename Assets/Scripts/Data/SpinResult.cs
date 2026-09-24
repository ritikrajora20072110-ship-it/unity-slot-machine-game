using System;
using SlotGame.Core;

namespace SlotGame.Data
{
    /// <summary>
    /// Represents the immutable outcome of a single spin evaluation.
    /// </summary>
    [Serializable]
    public class SpinResult
    {
        public SymbolType[] ReelSymbols { get; }
        public bool IsWin { get; }
        public int TotalPayoutMultiplier { get; }
        public int TotalCreditsWon { get; }
        public WinTier Tier { get; }
        public string WinDescription { get; }
        public bool IsBonusTriggered { get; }
        public int FreeSpinsAwarded { get; }

        public SpinResult(
            SymbolType[] reelSymbols,
            bool isWin,
            int totalPayoutMultiplier,
            int totalCreditsWon,
            WinTier tier,
            string winDescription,
            bool isBonusTriggered = false,
            int freeSpinsAwarded = 0)
        {
            ReelSymbols = reelSymbols ?? Array.Empty<SymbolType>();
            IsWin = isWin;
            TotalPayoutMultiplier = totalPayoutMultiplier;
            TotalCreditsWon = totalCreditsWon;
            Tier = tier;
            WinDescription = winDescription ?? string.Empty;
            IsBonusTriggered = isBonusTriggered;
            FreeSpinsAwarded = freeSpinsAwarded;
        }

        public static SpinResult CreateLoss(SymbolType[] reelSymbols)
        {
            return new SpinResult(
                reelSymbols,
                isWin: false,
                totalPayoutMultiplier: 0,
                totalCreditsWon: 0,
                tier: WinTier.None,
                winDescription: "No Match - Try Again!");
        }
    }
}
