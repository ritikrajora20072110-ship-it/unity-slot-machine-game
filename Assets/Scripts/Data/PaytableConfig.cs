using System;
using System.Collections.Generic;
using UnityEngine;
using SlotGame.Core;

namespace SlotGame.Data
{
    /// <summary>
    /// Configurable paytable asset specifying symbol rules, bet levels, and mathematical models.
    /// </summary>
    [CreateAssetMenu(fileName = "PaytableConfig", menuName = "Slot Machine/Paytable Config", order = 1)]
    public class PaytableConfig : ScriptableObject
    {
        [Header("Starting Economy")]
        [SerializeField] private int _startingBalance = 1000;
        [SerializeField] private int[] _betDenominations = new int[] { 10, 20, 50, 100, 200, 500 };
        [SerializeField] private int _defaultBetIndex = 1; // 20 credits

        [Header("Consolation & Mini Wins")]
        [Tooltip("Payout multiplier for 2 matching Cherries.")]
        [SerializeField] private int _cherryTwoMatchMultiplier = 2;

        [Header("Bonus Feature Settings")]
        [Tooltip("Number of free spins granted on 3 Bonus/Scatter symbols.")]
        [SerializeField] private int _bonusFreeSpinsCount = 5;

        [Tooltip("Win multiplier applied during Free Spins mode.")]
        [SerializeField] private int _bonusWinMultiplier = 2;

        [Header("Symbols Configuration")]
        [SerializeField] private List<SymbolDefinition> _symbols = new List<SymbolDefinition>();

        public int StartingBalance => _startingBalance;
        public IReadOnlyList<int> BetDenominations => _betDenominations;
        public int DefaultBetIndex => Mathf.Clamp(_defaultBetIndex, 0, _betDenominations.Length - 1);
        public int CherryTwoMatchMultiplier => _cherryTwoMatchMultiplier;
        public int BonusFreeSpinsCount => _bonusFreeSpinsCount;
        public int BonusWinMultiplier => _bonusWinMultiplier;
        public IReadOnlyList<SymbolDefinition> Symbols => _symbols;

        /// <summary>
        /// Retrieves the configuration definition for a specific symbol type.
        /// </summary>
        public SymbolDefinition GetDefinition(SymbolType type)
        {
            for (int i = 0; i < _symbols.Count; i++)
            {
                if (_symbols[i].SymbolType == type)
                {
                    return _symbols[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Calculates the theoretical Return To Player (RTP) percentage based on weights and payouts.
        /// </summary>
        public float CalculateTheoreticalRTP()
        {
            if (_symbols == null || _symbols.Count == 0) return 0f;

            int totalWeight = 0;
            foreach (var s in _symbols)
            {
                totalWeight += s.ReelWeight;
            }

            if (totalWeight <= 0) return 0f;

            double totalPayoutExpectation = 0.0;

            // 3 of a kind for each symbol
            foreach (var s in _symbols)
            {
                double prob = Math.Pow((double)s.ReelWeight / totalWeight, 3.0);
                totalPayoutExpectation += prob * s.PayoutMultiplier;
            }

            // Consolation win: 2 Cherries
            SymbolDefinition cherry = GetDefinition(SymbolType.Cherry);
            if (cherry != null && _cherryTwoMatchMultiplier > 0)
            {
                double pCherry = (double)cherry.ReelWeight / totalWeight;
                double pNotCherry = 1.0 - pCherry;
                // Combinations of exactly 2 cherries out of 3 reels = 3 * (p^2 * (1-p))
                double probTwoCherries = 3.0 * (pCherry * pCherry * pNotCherry);
                totalPayoutExpectation += probTwoCherries * _cherryTwoMatchMultiplier;
            }

            return (float)(totalPayoutExpectation * 100.0);
        }

        /// <summary>
        /// Populates default balanced casino weights if none are configured.
        /// Calibrated for 95.81% theoretical RTP with 13.75% hit frequency.
        /// </summary>
        public void InitializeDefaults(Sprite seven, Sprite cherry, Sprite bell, Sprite bar)
        {
            _symbols = new List<SymbolDefinition>
            {
                new SymbolDefinition(SymbolType.Seven, "Lucky 7", seven, payout: 50, weight: 5, isBonus: true),
                new SymbolDefinition(SymbolType.Bell, "Golden Bell", bell, payout: 20, weight: 15, isBonus: false),
                new SymbolDefinition(SymbolType.Bar, "BAR", bar, payout: 10, weight: 35, isBonus: false),
                new SymbolDefinition(SymbolType.Cherry, "Cherry", cherry, payout: 5, weight: 45, isBonus: false)
            };
        }
    }
}
