using System;
using UnityEngine;
using SlotGame.Core;

namespace SlotGame.Data
{
    /// <summary>
    /// Configuration data for a single slot symbol including visuals, payouts, and RNG weights.
    /// </summary>
    [Serializable]
    public class SymbolDefinition
    {
        [Header("Symbol Identity")]
        [SerializeField] private SymbolType _symbolType;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _symbolSprite;

        [Header("Payout & Probabilities")]
        [Tooltip("Payout multiplier awarded when 3 matching symbols land on the payline.")]
        [SerializeField] private int _payoutMultiplier = 10;

        [Tooltip("Relative probability weight on the virtual reel strip. Higher weight = lands more frequently.")]
        [SerializeField] private int _reelWeight = 20;

        [Tooltip("If true, this symbol can trigger Free Spins bonus game.")]
        [SerializeField] private bool _isBonusSymbol = false;

        public SymbolType SymbolType => _symbolType;
        public string DisplayName => _displayName;
        public Sprite SymbolSprite => _symbolSprite;
        public int PayoutMultiplier => _payoutMultiplier;
        public int ReelWeight => _reelWeight;
        public bool IsBonusSymbol => _isBonusSymbol;

        public SymbolDefinition() { }

        public SymbolDefinition(SymbolType type, string name, Sprite sprite, int payout, int weight, bool isBonus = false)
        {
            _symbolType = type;
            _displayName = name;
            _symbolSprite = sprite;
            _payoutMultiplier = payout;
            _reelWeight = weight;
            _isBonusSymbol = isBonus;
        }
    }
}
