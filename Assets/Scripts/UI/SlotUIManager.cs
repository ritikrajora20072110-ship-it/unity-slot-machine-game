using UnityEngine;
using UnityEngine.UI;
using SlotGame.Core;
using SlotGame.Data;
using SlotGame.Controllers;
using SlotGame.Economy;
using SlotGame.Audio;

namespace SlotGame.UI
{
    /// <summary>
    /// Master UI manager updating HUD elements, credits, bets, win banners, and button states.
    /// </summary>
    public class SlotUIManager : MonoBehaviour
    {
        [Header("Controllers")]
        [SerializeField] private SlotMachineController _slotMachine;
        [SerializeField] private EconomyManager _economyManager;

        [Header("HUD Text Displays")]
        [SerializeField] private Text _creditsText;
        [SerializeField] private Text _betText;
        [SerializeField] private Text _winText;
        [SerializeField] private Text _statusMessageText;

        [Header("Buttons")]
        [SerializeField] private Button _spinButton;
        [SerializeField] private Button _betMinusButton;
        [SerializeField] private Button _betPlusButton;
        [SerializeField] private Button _maxBetButton;
        [SerializeField] private Button _autoSpinButton;
        [SerializeField] private Button _paytableInfoButton;
        [SerializeField] private Button _audioMuteButton;

        [Header("Badges & Labels")]
        [SerializeField] private Text _autoSpinBadgeText;
        [SerializeField] private Text _freeSpinsBadgeText;
        [SerializeField] private GameObject _freeSpinsBanner;

        [Header("Modals")]
        [SerializeField] private PaytableModalUI _paytableModal;

        [Header("Particle Celebrations")]
        [SerializeField] private ParticleSystem _winParticles;

        private void Start()
        {
            BindEventListeners();
            UpdateStatusMessage("PULL LEVER OR PRESS SPIN TO PLAY!");
        }

        private void BindEventListeners()
        {
            if (_slotMachine != null)
            {
                _slotMachine.OnStateChanged += HandleStateChanged;
                _slotMachine.OnSpinCompleted += HandleSpinCompleted;
                _slotMachine.OnAutoSpinStateChanged += HandleAutoSpinStateChanged;
                _slotMachine.OnFreeSpinsStateChanged += HandleFreeSpinsStateChanged;
            }

            if (_economyManager != null)
            {
                _economyManager.OnBalanceChanged += HandleBalanceChanged;
                _economyManager.OnBetChanged += HandleBetChanged;
                _economyManager.OnWinAmountCounted += HandleWinAmountCounted;
                _economyManager.OnInsufficientFunds += HandleInsufficientFunds;
            }

            // Buttons
            if (_spinButton != null) _spinButton.onClick.AddListener(() => _slotMachine?.OnSpinButtonClicked());
            if (_betMinusButton != null) _betMinusButton.onClick.AddListener(() => { AudioManager.Instance?.PlayButtonClick(); _economyManager?.DecreaseBet(); });
            if (_betPlusButton != null) _betPlusButton.onClick.AddListener(() => { AudioManager.Instance?.PlayButtonClick(); _economyManager?.IncreaseBet(); });
            if (_maxBetButton != null) _maxBetButton.onClick.AddListener(() => { AudioManager.Instance?.PlayButtonClick(); _economyManager?.SetMaxBet(); });
            if (_autoSpinButton != null) _autoSpinButton.onClick.AddListener(() => { AudioManager.Instance?.PlayButtonClick(); _slotMachine?.ToggleAutoSpin(10); });
            if (_paytableInfoButton != null) _paytableInfoButton.onClick.AddListener(() => _paytableModal?.ToggleModal());
            if (_audioMuteButton != null) _audioMuteButton.onClick.AddListener(() => AudioManager.Instance?.ToggleMute());
        }

        private void HandleStateChanged(SlotMachineState state)
        {
            bool isIdle = (state == SlotMachineState.Idle);

            if (_spinButton != null) _spinButton.interactable = isIdle;
            if (_betMinusButton != null) _betMinusButton.interactable = isIdle;
            if (_betPlusButton != null) _betPlusButton.interactable = isIdle;
            if (_maxBetButton != null) _maxBetButton.interactable = isIdle;

            switch (state)
            {
                case SlotMachineState.Spinning:
                    UpdateStatusMessage("REELS SPINNING... GOOD LUCK!");
                    if (_winText != null) _winText.text = "0";
                    break;
                case SlotMachineState.Stopping:
                    UpdateStatusMessage("WATCH THE REELS...");
                    break;
                case SlotMachineState.Evaluating:
                    UpdateStatusMessage("EVALUATING PAYLINE...");
                    break;
                case SlotMachineState.Idle:
                    if (!_slotMachine.IsFreeSpinsActive && !_slotMachine.IsAutoSpinning)
                    {
                        UpdateStatusMessage("READY! PLACE YOUR BET AND SPIN!");
                    }
                    break;
            }
        }

        private void HandleSpinCompleted(SpinResult result)
        {
            if (result.IsWin)
            {
                UpdateStatusMessage(result.WinDescription);

                if (_winParticles != null && result.Tier >= WinTier.Medium)
                {
                    _winParticles.Play();
                }
            }
            else
            {
                UpdateStatusMessage("NO WIN THIS TIME. SPIN AGAIN!");
            }
        }

        private void HandleBalanceChanged(int balance)
        {
            if (_creditsText != null) _creditsText.text = balance.ToString("N0");
        }

        private void HandleBetChanged(int bet)
        {
            if (_betText != null) _betText.text = bet.ToString("N0");
        }

        private void HandleWinAmountCounted(int displayWin, int targetWin)
        {
            if (_winText != null)
            {
                _winText.text = displayWin.ToString("N0");
            }
        }

        private void HandleInsufficientFunds()
        {
            UpdateStatusMessage("INSUFFICIENT CREDITS! LOWER YOUR BET.");
        }

        private void HandleAutoSpinStateChanged(bool active, int remaining)
        {
            if (_autoSpinBadgeText != null)
            {
                _autoSpinBadgeText.gameObject.SetActive(active);
                _autoSpinBadgeText.text = active ? $"AUTO: {remaining}" : "AUTO";
            }
        }

        private void HandleFreeSpinsStateChanged(bool active, int remaining)
        {
            if (_freeSpinsBanner != null)
            {
                _freeSpinsBanner.SetActive(active);
            }

            if (_freeSpinsBadgeText != null)
            {
                _freeSpinsBadgeText.text = active ? $"FREE SPINS: {remaining} (2X MULTIPLIER)" : "";
            }

            if (active)
            {
                UpdateStatusMessage($"FREE SPINS BONUS ACTIVE! {remaining} SPINS LEFT (2x WINS)!");
            }
        }

        private void UpdateStatusMessage(string message)
        {
            if (_statusMessageText != null)
            {
                _statusMessageText.text = message;
            }
        }
    }
}
