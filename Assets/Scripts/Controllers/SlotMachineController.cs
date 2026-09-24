using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SlotGame.Core;
using SlotGame.Data;
using SlotGame.Reels;
using SlotGame.Economy;
using SlotGame.Audio;

namespace SlotGame.Controllers
{
    /// <summary>
    /// Master coordinator for the slot machine. Governs lifecycle state, staggered reel timing,
    /// anticipation suspense delays, win payouts, and bonus game transitions.
    /// </summary>
    public class SlotMachineController : MonoBehaviour
    {
        [Header("Configurations")]
        [SerializeField] private PaytableConfig _paytableConfig;

        [Header("Reel Strip Controllers")]
        [SerializeField] private List<ReelController> _reels = new List<ReelController>();

        [Header("Components")]
        [SerializeField] private LeverController _leverController;
        [SerializeField] private EconomyManager _economyManager;

        [Header("Timing Parameters")]
        [Tooltip("Minimum spin duration before the first reel begins deceleration.")]
        [SerializeField] private float _baseSpinDuration = 1.3f;

        [Tooltip("Staggered delay between consecutive reel stops.")]
        [SerializeField] private float _reelStaggerDelay = 0.45f;

        [Tooltip("Extra suspense spin time added to Reel 3 when Reel 1 & 2 match.")]
        [SerializeField] private float _anticipationDelay = 0.7f;

        public SlotMachineState CurrentState { get; private set; } = SlotMachineState.Idle;
        public bool IsAutoSpinning { get; private set; }
        public int RemainingAutoSpins { get; private set; }
        public bool IsFreeSpinsActive { get; private set; }
        public int RemainingFreeSpins { get; private set; }

        public event Action<SlotMachineState> OnStateChanged;
        public event Action<SpinResult> OnSpinCompleted;
        public event Action<bool, int> OnAutoSpinStateChanged; // (isActive, remainingCount)
        public event Action<bool, int> OnFreeSpinsStateChanged; // (isActive, remainingCount)

        private IRandomNumberGenerator _rng;
        private WinningLogicEvaluator _evaluator;
        private Coroutine _spinSequenceCoroutine;
        private SymbolType[] _targetSymbols = new SymbolType[3];

        private void Awake()
        {
            // Inject Cryptographic RNG for casino-grade fairness
            _rng = new CryptographicRNG();
        }

        private void Start()
        {
            if (_paytableConfig != null)
            {
                _evaluator = new WinningLogicEvaluator(_paytableConfig);
            }

            if (_economyManager != null && _paytableConfig != null)
            {
                _economyManager.Initialize(_paytableConfig);
            }

            if (_leverController != null)
            {
                _leverController.OnLeverPulled += HandleLeverPulled;
            }

            // Bind reel event listeners
            for (int i = 0; i < _reels.Count; i++)
            {
                int index = i;
                _reels[i].OnSymbolPassed += () => AudioManager.Instance?.PlayReelTick();
            }

            SetState(SlotMachineState.Idle);
        }

        private void SetState(SlotMachineState newState)
        {
            CurrentState = newState;
            if (_leverController != null)
            {
                _leverController.IsInteractable = (newState == SlotMachineState.Idle);
            }
            OnStateChanged?.Invoke(CurrentState);
        }

        private void HandleLeverPulled()
        {
            AudioManager.Instance?.PlayLeverPull();
            TryInitiateSpin();
        }

        /// <summary>
        /// Attempts to trigger a spin from the UI Spin Button.
        /// </summary>
        public void OnSpinButtonClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            if (_leverController != null && CurrentState == SlotMachineState.Idle)
            {
                _leverController.PullLever(); // Lever animation dispatches spin
            }
            else
            {
                TryInitiateSpin();
            }
        }

        /// <summary>
        /// Core spin trigger logic. Validates balance or free spin eligibility.
        /// </summary>
        public bool TryInitiateSpin()
        {
            if (CurrentState != SlotMachineState.Idle) return false;

            // Free spins bypass economy deduction
            if (!IsFreeSpinsActive)
            {
                if (_economyManager != null && !_economyManager.DeductBet())
                {
                    StopAutoSpin();
                    return false;
                }
            }
            else
            {
                RemainingFreeSpins--;
                OnFreeSpinsStateChanged?.Invoke(true, RemainingFreeSpins);
            }

            if (_spinSequenceCoroutine != null) StopCoroutine(_spinSequenceCoroutine);
            _spinSequenceCoroutine = StartCoroutine(SpinSequenceRoutine());
            return true;
        }

        private IEnumerator SpinSequenceRoutine()
        {
            SetState(SlotMachineState.Spinning);

            // Clear previous win highlights
            for (int i = 0; i < _reels.Count; i++)
            {
                _reels[i].HighlightCenterWin(false);
            }

            // 1. Determine predetermined random outcomes via RNG
            _targetSymbols = GeneratePredeterminedSymbols();

            // 2. Start audio spin loop and launch all reels
            AudioManager.Instance?.StartSpinLoop();
            for (int i = 0; i < _reels.Count; i++)
            {
                _reels[i].StartSpin();
            }

            // 3. Minimum base spin duration
            yield return new WaitForSeconds(_baseSpinDuration);

            // 4. Staggered reel stop sequence
            SetState(SlotMachineState.Stopping);

            // Stop Reel 0 (Left)
            bool reel0Stopped = false;
            Action<ReelController, SymbolType> onR0Stop = null;
            onR0Stop = (r, s) => { reel0Stopped = true; r.OnReelStopped -= onR0Stop; };
            _reels[0].OnReelStopped += onR0Stop;
            _reels[0].StopAtSymbol(_targetSymbols[0]);
            AudioManager.Instance?.PlayReelStop(0);

            yield return new WaitForSeconds(_reelStaggerDelay);

            // Stop Reel 1 (Middle)
            bool reel1Stopped = false;
            Action<ReelController, SymbolType> onR1Stop = null;
            onR1Stop = (r, s) => { reel1Stopped = true; r.OnReelStopped -= onR1Stop; };
            _reels[1].OnReelStopped += onR1Stop;
            _reels[1].StopAtSymbol(_targetSymbols[1]);
            AudioManager.Instance?.PlayReelStop(1);

            // Anticipation Tension Check:
            // If Reel 0 and Reel 1 match, build suspense for the final reel!
            float delayBeforeReel2 = _reelStaggerDelay;
            bool anticipationActive = (_targetSymbols[0] == _targetSymbols[1]);
            if (anticipationActive)
            {
                delayBeforeReel2 += _anticipationDelay;
            }

            yield return new WaitForSeconds(delayBeforeReel2);

            // Stop Reel 2 (Right)
            bool reel2Stopped = false;
            Action<ReelController, SymbolType> onR2Stop = null;
            onR2Stop = (r, s) => { reel2Stopped = true; r.OnReelStopped -= onR2Stop; };
            _reels[2].OnReelStopped += onR2Stop;
            _reels[2].StopAtSymbol(_targetSymbols[2]);
            AudioManager.Instance?.PlayReelStop(2);

            // Wait until reel 2 finishes bounce-back settling
            while (!reel2Stopped) yield return null;

            AudioManager.Instance?.StopSpinLoop();

            // 5. Outcome Evaluation Phase
            SetState(SlotMachineState.Evaluating);
            int currentBet = _economyManager != null ? _economyManager.CurrentBet : 10;
            SpinResult result = _evaluator.Evaluate(_targetSymbols, currentBet, IsFreeSpinsActive);

            if (result.IsWin)
            {
                SetState(SlotMachineState.WinCelebration);

                // Highlight matching symbols on the payline
                for (int i = 0; i < _reels.Count; i++)
                {
                    _reels[i].HighlightCenterWin(true);
                }

                AudioManager.Instance?.PlayWin(result.Tier);

                if (_economyManager != null)
                {
                    _economyManager.AwardWin(result.TotalCreditsWon);
                }

                // Check for Free Spins bonus trigger
                if (result.IsBonusTriggered)
                {
                    RemainingFreeSpins += result.FreeSpinsAwarded;
                    IsFreeSpinsActive = true;
                    OnFreeSpinsStateChanged?.Invoke(true, RemainingFreeSpins);
                }

                yield return new WaitForSeconds(1.8f);
            }

            // Check if free spins ended
            if (IsFreeSpinsActive && RemainingFreeSpins <= 0)
            {
                IsFreeSpinsActive = false;
                OnFreeSpinsStateChanged?.Invoke(false, 0);
            }

            OnSpinCompleted?.Invoke(result);
            SetState(SlotMachineState.Idle);

            // 6. Auto-Spin Logic
            if (IsAutoSpinning)
            {
                RemainingAutoSpins--;
                OnAutoSpinStateChanged?.Invoke(true, RemainingAutoSpins);

                if (RemainingAutoSpins <= 0 || (_economyManager != null && !_economyManager.CanAffordBet()))
                {
                    StopAutoSpin();
                }
                else
                {
                    yield return new WaitForSeconds(0.6f);
                    TryInitiateSpin();
                }
            }
            else if (IsFreeSpinsActive && RemainingFreeSpins > 0)
            {
                yield return new WaitForSeconds(0.8f);
                TryInitiateSpin();
            }
        }

        private SymbolType[] GeneratePredeterminedSymbols()
        {
            SymbolType[] results = new SymbolType[3];
            int count = _paytableConfig.Symbols.Count;
            int[] weights = new int[count];
            for (int i = 0; i < count; i++)
            {
                weights[i] = _paytableConfig.Symbols[i].ReelWeight;
            }

            for (int r = 0; r < 3; r++)
            {
                int selectedIdx = _rng.NextWeightedIndex(weights);
                results[r] = _paytableConfig.Symbols[selectedIdx].SymbolType;
            }

            return results;
        }

        public void StartAutoSpin(int spinCount)
        {
            if (spinCount <= 0) return;
            IsAutoSpinning = true;
            RemainingAutoSpins = spinCount;
            OnAutoSpinStateChanged?.Invoke(true, RemainingAutoSpins);

            if (CurrentState == SlotMachineState.Idle)
            {
                TryInitiateSpin();
            }
        }

        public void StopAutoSpin()
        {
            IsAutoSpinning = false;
            RemainingAutoSpins = 0;
            OnAutoSpinStateChanged?.Invoke(false, 0);
        }

        public void ToggleAutoSpin(int defaultCount = 10)
        {
            if (IsAutoSpinning)
            {
                StopAutoSpin();
            }
            else
            {
                StartAutoSpin(defaultCount);
            }
        }
    }
}
