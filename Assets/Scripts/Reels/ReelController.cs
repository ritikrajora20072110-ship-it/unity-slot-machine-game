using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SlotGame.Core;
using SlotGame.Data;

namespace SlotGame.Reels
{
    /// <summary>
    /// Manages an individual slot reel with physics-based acceleration, infinite symbol recycling,
    /// anticipation anticipation pullback, and mechanical bounce-back easing.
    /// </summary>
    public class ReelController : MonoBehaviour
    {
        [Header("Reel Configuration")]
        [Tooltip("Index of this reel (0 = left, 1 = middle, 2 = right).")]
        [SerializeField] private int _reelIndex = 0;

        [Tooltip("Vertical distance between adjacent symbols.")]
        [SerializeField] private float _symbolSpacing = 100f;

        [Tooltip("Total number of active visual cells in the recycling strip (minimum 5).")]
        [SerializeField] private int _cellCount = 5;

        [Header("Spin Dynamics")]
        [Tooltip("Maximum downward velocity in pixels per second.")]
        [SerializeField] private float _maxSpinSpeed = 2200f;

        [Tooltip("Acceleration time to reach max speed.")]
        [SerializeField] private float _accelerationTime = 0.35f;

        [Tooltip("Anticipation pullback distance before launching.")]
        [SerializeField] private float _pullbackDistance = 25f;

        [Tooltip("Anticipation pullback duration.")]
        [SerializeField] private float _pullbackDuration = 0.18f;

        [Tooltip("Bounce-back overshoot distance when snapping to stop.")]
        [SerializeField] private float _bounceDistance = 20f;

        [Tooltip("Bounce-back settle duration.")]
        [SerializeField] private float _bounceDuration = 0.28f;

        [Header("Prefabs & Parents")]
        [SerializeField] private GameObject _symbolCellPrefab;
        [SerializeField] private RectTransform _reelContainer;

        public int ReelIndex => _reelIndex;
        public bool IsSpinning { get; private set; }
        public SymbolType CenterSymbol => _centerSymbol;

        public event Action<ReelController> OnReelSpinStarted;
        public event Action<ReelController, SymbolType> OnReelStopped;
        public event Action OnSymbolPassed; // Audio tick event

        private List<ReelSymbol> _symbolsList = new List<ReelSymbol>();
        private List<RectTransform> _rectTransforms = new List<RectTransform>();
        private PaytableConfig _paytableConfig;
        private IRandomNumberGenerator _rng;
        private SymbolType _centerSymbol = SymbolType.Cherry;

        private Coroutine _spinCoroutine;
        private float _topThreshold;
        private float _bottomThreshold;

        public void Initialize(int index, PaytableConfig config, IRandomNumberGenerator rng, GameObject cellPrefab, RectTransform container)
        {
            _reelIndex = index;
            _paytableConfig = config;
            _rng = rng;
            if (cellPrefab != null) _symbolCellPrefab = cellPrefab;
            if (container != null) _reelContainer = container;

            SetupSymbols();
        }

        private void Awake()
        {
            if (_reelContainer == null)
            {
                _reelContainer = GetComponent<RectTransform>();
            }
        }

        private void SetupSymbols()
        {
            // Clear existing if any
            for (int i = 0; i < _symbolsList.Count; i++)
            {
                if (_symbolsList[i] != null) Destroy(_symbolsList[i].gameObject);
            }
            _symbolsList.Clear();
            _rectTransforms.Clear();

            int halfCells = _cellCount / 2;
            _topThreshold = (halfCells + 1) * _symbolSpacing;
            _bottomThreshold = -(halfCells + 1) * _symbolSpacing;

            for (int i = 0; i < _cellCount; i++)
            {
                int offsetFromCenter = i - halfCells; // e.g. -2, -1, 0, 1, 2
                float yPos = offsetFromCenter * _symbolSpacing;

                GameObject obj;
                if (_symbolCellPrefab != null)
                {
                    obj = Instantiate(_symbolCellPrefab, _reelContainer);
                }
                else
                {
                    obj = new GameObject($"ReelSymbol_{i}", typeof(RectTransform), typeof(ReelSymbol));
                    obj.transform.SetParent(_reelContainer, false);
                }

                RectTransform rt = obj.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, yPos);
                _rectTransforms.Add(rt);

                ReelSymbol symbol = obj.GetComponent<ReelSymbol>();
                if (symbol == null) symbol = obj.AddComponent<ReelSymbol>();
                _symbolsList.Add(symbol);

                // Initial random or fixed symbol
                SymbolType initialType = GetRandomSymbolType();
                Sprite sprite = GetSymbolSprite(initialType);
                symbol.SetSymbol(initialType, sprite);

                if (offsetFromCenter == 0)
                {
                    _centerSymbol = initialType;
                }
            }
        }

        /// <summary>
        /// Initiates the reel spin sequence.
        /// </summary>
        public void StartSpin()
        {
            if (IsSpinning) return;

            if (_spinCoroutine != null) StopCoroutine(_spinCoroutine);
            _spinCoroutine = StartCoroutine(SpinRoutine());
        }

        private IEnumerator SpinRoutine()
        {
            IsSpinning = true;
            OnReelSpinStarted?.Invoke(this);

            // 1. Anticipation Pullback (upwards jerk before spinning down)
            if (_pullbackDistance > 0f)
            {
                float elapsed = 0f;
                Vector2[] basePositions = new Vector2[_rectTransforms.Count];
                for (int i = 0; i < _rectTransforms.Count; i++) basePositions[i] = _rectTransforms[i].anchoredPosition;

                while (elapsed < _pullbackDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / _pullbackDuration;
                    float curve = Mathf.Sin(t * Mathf.PI * 0.5f); // Ease out
                    float yOffset = _pullbackDistance * curve;

                    for (int i = 0; i < _rectTransforms.Count; i++)
                    {
                        _rectTransforms[i].anchoredPosition = new Vector2(basePositions[i].x, basePositions[i].y + yOffset);
                    }
                    yield return null;
                }
            }

            // 2. Acceleration Phase
            float currentSpeed = 0f;
            float accelElapsed = 0f;
            while (accelElapsed < _accelerationTime)
            {
                accelElapsed += Time.deltaTime;
                currentSpeed = Mathf.Lerp(0f, _maxSpinSpeed, accelElapsed / _accelerationTime);
                AdvanceReel(currentSpeed * Time.deltaTime);
                yield return null;
            }

            // 3. Constant High-Speed Rotation until Stop requested
            while (IsSpinning)
            {
                AdvanceReel(_maxSpinSpeed * Time.deltaTime);
                yield return null;
            }
        }

        /// <summary>
        /// Requests the reel to decelerate and smoothly land with target symbol centered.
        /// </summary>
        public void StopAtSymbol(SymbolType finalSymbol)
        {
            if (!IsSpinning) return;

            StartCoroutine(StopRoutine(finalSymbol));
        }

        private IEnumerator StopRoutine(SymbolType finalSymbol)
        {
            // Keep spinning until we align the strip for deceleration
            yield return new WaitForSeconds(0.1f);

            // Stop the constant spin loop
            IsSpinning = false;
            if (_spinCoroutine != null) StopCoroutine(_spinCoroutine);

            _centerSymbol = finalSymbol;

            // Align symbols to nearest slot positions
            int centerIndex = _cellCount / 2;
            int total = _symbolsList.Count;

            // Assign the target final symbol to the center slot
            _symbolsList[centerIndex].SetSymbol(finalSymbol, GetSymbolSprite(finalSymbol));

            // Assign neighboring symbols realistically
            for (int i = 0; i < total; i++)
            {
                if (i != centerIndex)
                {
                    SymbolType neighborType = GetRandomSymbolType();
                    _symbolsList[i].SetSymbol(neighborType, GetSymbolSprite(neighborType));
                }
            }

            // 4. Deceleration & Bounce-Back Settling (EaseOutBack)
            // Move slightly down beyond 0 (overshoot) then spring back up to 0
            float bounceElapsed = 0f;
            int halfCells = _cellCount / 2;

            while (bounceElapsed < _bounceDuration)
            {
                bounceElapsed += Time.deltaTime;
                float progress = bounceElapsed / _bounceDuration;

                // Dampened spring equation: overshoot and bounce back
                float springOffset = -_bounceDistance * Mathf.Sin(progress * Mathf.PI) * (1f - progress);

                for (int i = 0; i < _rectTransforms.Count; i++)
                {
                    int offset = i - halfCells;
                    float targetY = offset * _symbolSpacing;
                    _rectTransforms[i].anchoredPosition = new Vector2(0f, targetY + springOffset);
                }
                yield return null;
            }

            // Snap exactly to final positions
            for (int i = 0; i < _rectTransforms.Count; i++)
            {
                int offset = i - halfCells;
                _rectTransforms[i].anchoredPosition = new Vector2(0f, offset * _symbolSpacing);
            }

            OnReelStopped?.Invoke(this, _centerSymbol);
        }

        private void AdvanceReel(float deltaY)
        {
            int count = _rectTransforms.Count;
            for (int i = 0; i < count; i++)
            {
                RectTransform rt = _rectTransforms[i];
                float newY = rt.anchoredPosition.y - deltaY;

                // When symbol falls below bottom threshold, wrap it to top
                if (newY < _bottomThreshold)
                {
                    float overshoot = _bottomThreshold - newY;
                    newY = _topThreshold - overshoot;

                    // Assign fresh symbol on recycling
                    SymbolType nextType = GetRandomSymbolType();
                    _symbolsList[i].SetSymbol(nextType, GetSymbolSprite(nextType));

                    OnSymbolPassed?.Invoke();
                }

                rt.anchoredPosition = new Vector2(0f, newY);
            }
        }

        public void HighlightCenterWin(bool active)
        {
            int centerIndex = _cellCount / 2;
            if (centerIndex >= 0 && centerIndex < _symbolsList.Count)
            {
                _symbolsList[centerIndex].HighlightWin(active);
            }
        }

        private SymbolType GetRandomSymbolType()
        {
            if (_paytableConfig == null || _paytableConfig.Symbols == null || _paytableConfig.Symbols.Count == 0)
            {
                return (SymbolType)(_rng != null ? _rng.NextInt(0, 4) : UnityEngine.Random.Range(0, 4));
            }

            int count = _paytableConfig.Symbols.Count;
            int[] weights = new int[count];
            for (int i = 0; i < count; i++)
            {
                weights[i] = _paytableConfig.Symbols[i].ReelWeight;
            }

            int selected = _rng != null ? _rng.NextWeightedIndex(weights) : 0;
            return _paytableConfig.Symbols[selected].SymbolType;
        }

        private Sprite GetSymbolSprite(SymbolType type)
        {
            if (_paytableConfig != null)
            {
                var def = _paytableConfig.GetDefinition(type);
                if (def != null && def.SymbolSprite != null) return def.SymbolSprite;
            }
            return null;
        }
    }
}
