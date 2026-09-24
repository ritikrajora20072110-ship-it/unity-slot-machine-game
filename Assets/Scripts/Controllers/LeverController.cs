using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SlotGame.Controllers
{
    /// <summary>
    /// Interactive mechanical lever controller.
    /// Handles user clicks/taps, plays the pull down animation, and dispatches the spin trigger.
    /// </summary>
    public class LeverController : MonoBehaviour, IPointerDownHandler
    {
        [Header("Sprites")]
        [Tooltip("Lever in default UP position (slot-machine2).")]
        [SerializeField] private Sprite _leverUpSprite;

        [Tooltip("Lever in pulled DOWN position (slot-machine3).")]
        [SerializeField] private Sprite _leverDownSprite;

        [Header("Components")]
        [SerializeField] private Image _leverImage;
        [SerializeField] private SpriteRenderer _leverRenderer;

        [Header("Timing")]
        [Tooltip("How long the lever stays down before springing back up.")]
        [SerializeField] private float _pullDownDuration = 0.22f;

        public bool IsInteractable { get; set; } = true;

        public event Action OnLeverPulled;

        private bool _isPulling = false;

        private void Awake()
        {
            if (_leverImage == null) _leverImage = GetComponent<Image>();
            if (_leverRenderer == null) _leverRenderer = GetComponent<SpriteRenderer>();
            SetLeverState(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            PullLever();
        }

        /// <summary>
        /// Pulls the lever programmatically or via user interaction.
        /// </summary>
        public void PullLever()
        {
            if (!IsInteractable || _isPulling) return;
            StartCoroutine(PullRoutine());
        }

        private IEnumerator PullRoutine()
        {
            _isPulling = true;
            SetLeverState(true); // Down

            OnLeverPulled?.Invoke();

            yield return new WaitForSeconds(_pullDownDuration);

            SetLeverState(false); // Up
            _isPulling = false;
        }

        private void SetLeverState(bool down)
        {
            Sprite targetSprite = down ? _leverDownSprite : _leverUpSprite;

            if (_leverImage != null && targetSprite != null)
            {
                _leverImage.sprite = targetSprite;
            }

            if (_leverRenderer != null && targetSprite != null)
            {
                _leverRenderer.sprite = targetSprite;
            }
        }
    }
}
