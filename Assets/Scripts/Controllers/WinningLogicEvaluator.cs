using System;
using SlotGame.Core;
using SlotGame.Data;

namespace SlotGame.Controllers
{
    /// <summary>
    /// Pure logic evaluator for slot outcomes. Tested independently from Unity GameObject hierarchies.
    /// </summary>
    public class WinningLogicEvaluator
    {
        private readonly PaytableConfig _config;

        public WinningLogicEvaluator(PaytableConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Evaluates the center payline symbols across all reels.
        /// </summary>
        public SpinResult Evaluate(SymbolType[] reelSymbols, int currentBet, bool isFreeSpin = false)
        {
            if (reelSymbols == null || reelSymbols.Length < 3)
            {
                throw new ArgumentException("Reel symbols array must contain at least 3 symbols.");
            }

            SymbolType s0 = reelSymbols[0];
            SymbolType s1 = reelSymbols[1];
            SymbolType s2 = reelSymbols[2];

            // 1. Check for 3 of a kind (All reels matching)
            if (s0 == s1 && s1 == s2)
            {
                SymbolType matchSymbol = s0;
                var def = _config.GetDefinition(matchSymbol);
                int baseMultiplier = def != null ? def.PayoutMultiplier : 10;

                // Apply bonus free spin multiplier if active
                int effectiveMultiplier = isFreeSpin ? baseMultiplier * _config.BonusWinMultiplier : baseMultiplier;
                int creditsWon = currentBet * effectiveMultiplier;

                WinTier tier;
                string description;

                if (matchSymbol == SymbolType.Seven)
                {
                    tier = WinTier.Jackpot;
                    description = $"★★★ MEGA JACKPOT! 3x LUCKY SEVENS! ({effectiveMultiplier}x) ★★★";
                }
                else if (matchSymbol == SymbolType.Bell)
                {
                    tier = WinTier.Large;
                    description = $"★ BIG WIN! 3x GOLDEN BELLS! ({effectiveMultiplier}x) ★";
                }
                else if (matchSymbol == SymbolType.Bar)
                {
                    tier = WinTier.Medium;
                    description = $"★ NICE WIN! 3x TRIPLE BARS! ({effectiveMultiplier}x) ★";
                }
                else if (matchSymbol == SymbolType.Bonus)
                {
                    tier = WinTier.Jackpot;
                    description = $"★ BONUS FEATURE! {_config.BonusFreeSpinsCount} FREE SPINS AWARDED! ★";
                    return new SpinResult(reelSymbols, true, effectiveMultiplier, creditsWon, tier, description, isBonusTriggered: true, freeSpinsAwarded: _config.BonusFreeSpinsCount);
                }
                else // Cherry
                {
                    tier = WinTier.Medium;
                    description = $"★ SWEET WIN! 3x CHERRIES! ({effectiveMultiplier}x) ★";
                }

                return new SpinResult(reelSymbols, true, effectiveMultiplier, creditsWon, tier, description);
            }

            // 2. Consolation Win: 2 Cherries
            int cherryCount = 0;
            if (s0 == SymbolType.Cherry) cherryCount++;
            if (s1 == SymbolType.Cherry) cherryCount++;
            if (s2 == SymbolType.Cherry) cherryCount++;

            if (cherryCount == 2)
            {
                int baseMultiplier = _config.CherryTwoMatchMultiplier;
                int effectiveMultiplier = isFreeSpin ? baseMultiplier * _config.BonusWinMultiplier : baseMultiplier;
                int creditsWon = currentBet * effectiveMultiplier;

                return new SpinResult(
                    reelSymbols,
                    isWin: true,
                    totalPayoutMultiplier: effectiveMultiplier,
                    totalCreditsWon: creditsWon,
                    tier: WinTier.Small,
                    winDescription: $"Cherry Pair Win! ({effectiveMultiplier}x)");
            }

            // 3. Loss
            return SpinResult.CreateLoss(reelSymbols);
        }
    }
}
