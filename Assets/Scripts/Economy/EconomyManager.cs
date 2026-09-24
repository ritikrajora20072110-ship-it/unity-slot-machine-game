using System;
using System.Collections;
using UnityEngine;
using SlotGame.Data;

namespace SlotGame.Economy
{
    /// <summary>
    /// Manages player credits, bet selection, payout accumulation, and session statistics.
    /// Features smooth animated number roll-up for big wins.
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private PaytableConfig _paytableConfig;

        public int CurrentBalance { get; private set; }
        public int CurrentBet { get; private set; }
        public int CurrentBetIndex { get; private set; }
        public int LastWinAmount { get; private set; }

        public int TotalSpinsCount { get; private set; }
        public long TotalWagered { get; private set; }
        public long TotalWon { get; private set; }

        public event Action<int> OnBalanceChanged;
        public event Action<int> OnBetChanged;
        public event Action<int, int> OnWinAmountCounted; // (displayValue, targetValue)
        public event Action OnInsufficientFunds;

        private Coroutine _countUpRoutine;

        public void Initialize(PaytableConfig config)
        {
            _paytableConfig = config;
            CurrentBalance = _paytableConfig.StartingBalance;
            CurrentBetIndex = _paytableConfig.DefaultBetIndex;
            CurrentBet = _paytableConfig.BetDenominations[CurrentBetIndex];

            OnBalanceChanged?.Invoke(CurrentBalance);
            OnBetChanged?.Invoke(CurrentBet);
        }

        public bool CanAffordBet()
        {
            return CurrentBalance >= CurrentBet;
        }

        /// <summary>
        /// Deducts current bet from player balance.
        /// </summary>
        public bool DeductBet()
        {
            if (!CanAffordBet())
            {
                OnInsufficientFunds?.Invoke();
                return false;
            }

            CurrentBalance -= CurrentBet;
            TotalWagered += CurrentBet;
            TotalSpinsCount++;

            OnBalanceChanged?.Invoke(CurrentBalance);
            return true;
        }

        /// <summary>
        /// Increases bet to next denomination tier.
        /// </summary>
        public void IncreaseBet()
        {
            if (_paytableConfig == null) return;
            var denoms = _paytableConfig.BetDenominations;
            if (CurrentBetIndex < denoms.Count - 1)
            {
                CurrentBetIndex++;
                CurrentBet = denoms[CurrentBetIndex];
                OnBetChanged?.Invoke(CurrentBet);
            }
        }

        /// <summary>
        /// Decreases bet to previous denomination tier.
        /// </summary>
        public void DecreaseBet()
        {
            if (_paytableConfig == null) return;
            if (CurrentBetIndex > 0)
            {
                CurrentBetIndex--;
                CurrentBet = _paytableConfig.BetDenominations[CurrentBetIndex];
                OnBetChanged?.Invoke(CurrentBet);
            }
        }

        /// <summary>
        /// Sets bet to maximum available denomination.
        /// </summary>
        public void SetMaxBet()
        {
            if (_paytableConfig == null) return;
            var denoms = _paytableConfig.BetDenominations;
            CurrentBetIndex = denoms.Count - 1;
            CurrentBet = denoms[CurrentBetIndex];
            OnBetChanged?.Invoke(CurrentBet);
        }

        /// <summary>
        /// Credits won amount to balance with animated counter.
        /// </summary>
        public void AwardWin(int winAmount, bool animate = true)
        {
            LastWinAmount = winAmount;
            TotalWon += winAmount;

            if (animate && winAmount > 0)
            {
                if (_countUpRoutine != null) StopCoroutine(_countUpRoutine);
                _countUpRoutine = StartCoroutine(CountUpRoutine(winAmount));
            }
            else
            {
                CurrentBalance += winAmount;
                OnBalanceChanged?.Invoke(CurrentBalance);
                OnWinAmountCounted?.Invoke(winAmount, winAmount);
            }
        }

        private IEnumerator CountUpRoutine(int targetWin)
        {
            float duration = Mathf.Clamp(targetWin * 0.005f, 0.4f, 2.5f);
            float elapsed = 0f;
            int startBalance = CurrentBalance;
            int targetBalance = CurrentBalance + targetWin;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                int currentDisplayWin = Mathf.RoundToInt(Mathf.Lerp(0, targetWin, t));
                CurrentBalance = Mathf.RoundToInt(Mathf.Lerp(startBalance, targetBalance, t));

                OnWinAmountCounted?.Invoke(currentDisplayWin, targetWin);
                OnBalanceChanged?.Invoke(CurrentBalance);
                yield return null;
            }

            CurrentBalance = targetBalance;
            OnBalanceChanged?.Invoke(CurrentBalance);
            OnWinAmountCounted?.Invoke(targetWin, targetWin);
        }

        /// <summary>
        /// Resets player credits to default bankroll (e.g. reload bankroll).
        /// </summary>
        public void ResetBankroll()
        {
            CurrentBalance = _paytableConfig != null ? _paytableConfig.StartingBalance : 1000;
            OnBalanceChanged?.Invoke(CurrentBalance);
        }
    }
}
