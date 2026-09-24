using System;
using System.Diagnostics;
using SlotGame.Core;
using SlotGame.Data;
using SlotGame.Controllers;
using Debug = UnityEngine.Debug;

namespace SlotGame.Core
{
    /// <summary>
    /// Monte Carlo statistical verification tool.
    /// Runs large-scale spin simulations to verify empirical RTP against theoretical models.
    /// </summary>
    public static class SlotMathSimulator
    {
        public class SimulationReport
        {
            public int TotalSpins;
            public long TotalWagered;
            public long TotalWon;
            public double EmpiricalRTP;
            public double HitFrequencyPercent;
            public int JackpotCount;
            public int LargeWinCount;
            public int MediumWinCount;
            public int SmallWinCount;
            public double ElapsedMilliseconds;

            public override string ToString()
            {
                return $"[Simulation Report - {TotalSpins:N0} Spins]\n" +
                       $"Total Wagered: {TotalWagered:N0} credits\n" +
                       $"Total Won:     {TotalWon:N0} credits\n" +
                       $"Empirical RTP: {EmpiricalRTP:F2}%\n" +
                       $"Hit Frequency: {HitFrequencyPercent:F2}%\n" +
                       $"Jackpots (777): {JackpotCount:N0}\n" +
                       $"Large (Bells):  {LargeWinCount:N0}\n" +
                       $"Medium (Bars/Cherries): {MediumWinCount:N0}\n" +
                       $"Small (Cherry Pairs):   {SmallWinCount:N0}\n" +
                       $"Execution Time: {ElapsedMilliseconds:F1} ms";
            }
        }

        public static SimulationReport RunSimulation(PaytableConfig config, int spinCount = 500000, int bet = 10)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            IRandomNumberGenerator rng = new StandardRNG(42);
            WinningLogicEvaluator evaluator = new WinningLogicEvaluator(config);

            int totalSymbols = config.Symbols.Count;
            int[] weights = new int[totalSymbols];
            for (int i = 0; i < totalSymbols; i++) weights[i] = config.Symbols[i].ReelWeight;

            long totalWagered = 0;
            long totalWon = 0;
            int winCount = 0;
            int jackpots = 0;
            int largeWins = 0;
            int mediumWins = 0;
            int smallWins = 0;

            Stopwatch sw = Stopwatch.StartNew();

            SymbolType[] currentSymbols = new SymbolType[3];

            for (int i = 0; i < spinCount; i++)
            {
                totalWagered += bet;

                // Roll 3 reels
                for (int r = 0; r < 3; r++)
                {
                    int idx = rng.NextWeightedIndex(weights);
                    currentSymbols[r] = config.Symbols[idx].SymbolType;
                }

                SpinResult res = evaluator.Evaluate(currentSymbols, bet, isFreeSpin: false);
                if (res.IsWin)
                {
                    winCount++;
                    totalWon += res.TotalCreditsWon;

                    switch (res.Tier)
                    {
                        case WinTier.Jackpot: jackpots++; break;
                        case WinTier.Large: largeWins++; break;
                        case WinTier.Medium: mediumWins++; break;
                        case WinTier.Small: smallWins++; break;
                    }
                }
            }

            sw.Stop();

            var report = new SimulationReport
            {
                TotalSpins = spinCount,
                TotalWagered = totalWagered,
                TotalWon = totalWon,
                EmpiricalRTP = (double)totalWon / totalWagered * 100.0,
                HitFrequencyPercent = (double)winCount / spinCount * 100.0,
                JackpotCount = jackpots,
                LargeWinCount = largeWins,
                MediumWinCount = mediumWins,
                SmallWinCount = smallWins,
                ElapsedMilliseconds = sw.ElapsedMilliseconds
            };

            return report;
        }
    }
}
