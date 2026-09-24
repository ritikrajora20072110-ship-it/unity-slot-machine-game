using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SlotGame.Core;

namespace SlotGame.Reels
{
    /// <summary>
    /// Visual and state representation of an individual symbol in a reel cell.
    /// Supports UI Image and SpriteRenderer, highlighting, and bounce effects.
    /// </summary>
    public class ReelSymbol : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Image _uiImage;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Effects")]
        [SerializeField] private GameObject _winHighlightEffect;

        public SymbolType CurrentSymbolType { get; private set; }

        private Coroutine _pulseRoutine;
        private Vector3 _originalScale;

        private void Awake()
        {
            if (_uiImage == null) _uiImage = GetComponent<Image>();
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            _originalScale = transform.localScale;
        }

        /// <summary>
        /// Sets the symbol visual sprite and logical type.
        /// </summary>
        public void SetSymbol(SymbolType type, Sprite sprite)
        {
            CurrentSymbolType = type;

            if (_uiImage != null)
            {
                _uiImage.sprite = sprite;
                _uiImage.enabled = sprite != null;
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = sprite;
                _spriteRenderer.enabled = sprite != null;
            }
        }

        /// <summary>
        /// Plays pulsing win highlight animation.
        /// </summary>
        public void HighlightWin(bool active)
        {
            if (_winHighlightEffect != null)
            {
                _winHighlightEffect.SetActive(active);
            }

            if (active)
            {
                if (_pulseRoutine != null) StopCoroutine(_pulseRoutine);
                _pulseRoutine = StartCoroutine(PulseRoutine());
            }
            else
            {
                if (_pulseRoutine != null)
                {
                    StopCoroutine(_pulseRoutine);
                    _pulseRoutine = null;
                }
                transform.localScale = _originalScale;
            }
        }

        private IEnumerator PulseRoutine()
        {
            float duration = 0.4f;
            while (true)
            {
                // Scale up
                for (float t = 0; t < duration; t += Time.deltaTime)
                {
                    float factor = 1f + 0.15f * Mathf.Sin((t / duration) * Mathf.PI);
                    transform.localScale = _originalScale * factor;
                    yield return null;
                }
            }
        }
    }
}
