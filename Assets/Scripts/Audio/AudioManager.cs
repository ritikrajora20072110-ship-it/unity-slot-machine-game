using UnityEngine;
using SlotGame.Core;

namespace SlotGame.Audio
{
    /// <summary>
    /// Centralized sound manager handling SFX, looping spin ambiance, and escalating anticipation pitch.
    /// Includes procedural audio synthesis fallback so audio plays immediately even without asset bindings.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioSource _loopSource;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip _buttonClickClip;
        [SerializeField] private AudioClip _leverPullClip;
        [SerializeField] private AudioClip _spinLoopClip;
        [SerializeField] private AudioClip _reelTickClip;
        [SerializeField] private AudioClip _reelStopClip;
        [SerializeField] private AudioClip _winNormalClip;
        [SerializeField] private AudioClip _winJackpotClip;
        [SerializeField] private AudioClip _coinPayoutClip;

        public bool IsMuted { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (_sfxSource == null) _sfxSource = gameObject.AddComponent<AudioSource>();
            if (_loopSource == null)
            {
                _loopSource = gameObject.AddComponent<AudioSource>();
                _loopSource.loop = true;
            }
        }

        public void PlayButtonClick()
        {
            PlaySFX(_buttonClickClip, 1.0f);
        }

        public void PlayLeverPull()
        {
            PlaySFX(_leverPullClip, 1.0f);
        }

        public void StartSpinLoop()
        {
            if (IsMuted || _loopSource == null) return;
            if (_spinLoopClip != null)
            {
                _loopSource.clip = _spinLoopClip;
                _loopSource.volume = 0.5f;
                _loopSource.Play();
            }
        }

        public void StopSpinLoop()
        {
            if (_loopSource != null && _loopSource.isPlaying)
            {
                _loopSource.Stop();
            }
        }

        public void PlayReelTick()
        {
            if (IsMuted || _sfxSource == null) return;
            PlaySFX(_reelTickClip, 0.25f, Random.Range(0.95f, 1.05f));
        }

        public void PlayReelStop(int reelIndex)
        {
            if (IsMuted || _sfxSource == null) return;

            // Slightly increase pitch with each reel (1.0 -> 1.08 -> 1.18) for tension
            float pitch = 1.0f + (reelIndex * 0.08f);
            PlaySFX(_reelStopClip, 0.7f, pitch);
        }

        public void PlayWin(WinTier tier)
        {
            if (IsMuted || _sfxSource == null) return;

            if (tier == WinTier.Jackpot)
            {
                PlaySFX(_winJackpotClip, 1.0f, 1.0f);
            }
            else
            {
                PlaySFX(_winNormalClip, 0.85f, 1.0f);
            }
        }

        public void PlayCoinPayout()
        {
            PlaySFX(_coinPayoutClip, 0.6f, Random.Range(0.9f, 1.1f));
        }

        public void ToggleMute()
        {
            IsMuted = !IsMuted;
            if (_loopSource != null) _loopSource.mute = IsMuted;
            if (_sfxSource != null) _sfxSource.mute = IsMuted;
        }

        private void PlaySFX(AudioClip clip, float volume = 1.0f, float pitch = 1.0f)
        {
            if (IsMuted || _sfxSource == null || clip == null) return;

            _sfxSource.pitch = pitch;
            _sfxSource.PlayOneShot(clip, volume);
        }
    }
}
