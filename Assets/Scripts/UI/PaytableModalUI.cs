using UnityEngine;
using UnityEngine.UI;
using SlotGame.Audio;

namespace SlotGame.UI
{
    /// <summary>
    /// Manages the Paytable / Rules popup dialog using the provided popup.png asset.
    /// </summary>
    public class PaytableModalUI : MonoBehaviour
    {
        [Header("Modal Root")]
        [SerializeField] private GameObject _modalRoot;

        [Header("Buttons")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _confirmButton;

        private void Awake()
        {
            if (_closeButton != null) _closeButton.onClick.AddListener(HideModal);
            if (_confirmButton != null) _confirmButton.onClick.AddListener(HideModal);
        }

        public void ShowModal()
        {
            AudioManager.Instance?.PlayButtonClick();
            if (_modalRoot != null)
            {
                _modalRoot.SetActive(true);
            }
        }

        public void HideModal()
        {
            AudioManager.Instance?.PlayButtonClick();
            if (_modalRoot != null)
            {
                _modalRoot.SetActive(false);
            }
        }

        public void ToggleModal()
        {
            if (_modalRoot != null)
            {
                if (_modalRoot.activeSelf) HideModal();
                else ShowModal();
            }
        }
    }
}
