using System;
using System.Collections.Generic;
using UnityEngine;
using KBTV.Core;
using KBTV.Callers;
using KBTV.Managers;

namespace KBTV.Audio
{
    /// <summary>
    /// Types of sound effects in the game.
    /// </summary>
    public enum SFXType
    {
        // Phase transitions
        ShowStart,          // Live show begins
        ShowEnd,            // Live show ends
        
        // Caller events
        CallerIncoming,     // New caller in queue
        CallerApproved,     // Caller approved (put on hold)
        CallerRejected,     // Caller rejected
        CallerOnAir,        // Caller goes on air
        CallerComplete,     // Call ends successfully
        CallerDisconnect,   // Caller hangs up (patience ran out)
        
        // UI feedback
        ButtonClick,        // Generic button press
        ItemUsed,           // Item consumed
        ItemEmpty,          // Tried to use item with no stock
        
        // Alerts
        LowStat,            // Stat dropped critically low
        HighListeners,      // Reached new peak listeners
        
        // Ambience
        PhoneRing,          // Phone ringing loop
        StaticBurst         // Radio static
    }

    /// <summary>
    /// Manages all game audio - SFX playback, music, and ambience.
    /// Subscribes to game events and plays appropriate sounds.
    /// </summary>
    public class AudioManager : SingletonMonoBehaviour<AudioManager>
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _ambienceSource;

        [Header("Volume Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float _masterVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float _sfxVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float _musicVolume = 0.5f;
        [Range(0f, 1f)]
        [SerializeField] private float _ambienceVolume = 0.3f;

        [Header("SFX Clips")]
        [SerializeField] private AudioClip _showStartClip;
        [SerializeField] private AudioClip _showEndClip;
        [SerializeField] private AudioClip _callerIncomingClip;
        [SerializeField] private AudioClip _callerApprovedClip;
        [SerializeField] private AudioClip _callerRejectedClip;
        [SerializeField] private AudioClip _callerOnAirClip;
        [SerializeField] private AudioClip _callerCompleteClip;
        [SerializeField] private AudioClip _callerDisconnectClip;
        [SerializeField] private AudioClip _buttonClickClip;
        [SerializeField] private AudioClip _itemUsedClip;
        [SerializeField] private AudioClip _itemEmptyClip;
        [SerializeField] private AudioClip _lowStatClip;
        [SerializeField] private AudioClip _highListenersClip;
        [SerializeField] private AudioClip _phoneRingClip;
        [SerializeField] private AudioClip _staticBurstClip;

        // Cache for quick lookup
        private Dictionary<SFXType, AudioClip> _sfxClips;

        // Event subscriptions
        private GameStateManager _gameState;
        private CallerQueue _callerQueue;
        private ListenerManager _listenerManager;
        private ItemManager _itemManager;

        public float MasterVolume
        {
            get => _masterVolume;
            set => _masterVolume = Mathf.Clamp01(value);
        }

        public float SFXVolume
        {
            get => _sfxVolume;
            set => _sfxVolume = Mathf.Clamp01(value);
        }

        public float MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = Mathf.Clamp01(value);
                if (_musicSource != null)
                    _musicSource.volume = _musicVolume * _masterVolume;
            }
        }

        protected override void OnSingletonAwake()
        {
            SetupAudioSources();
            BuildClipDictionary();
        }

        private void Start()
        {
            SubscribeToEvents();
        }

        protected override void OnDestroy()
        {
            UnsubscribeFromEvents();
            base.OnDestroy();
        }

        private void SetupAudioSources()
        {
            // Create SFX source if not assigned
            if (_sfxSource == null)
            {
                GameObject sfxObj = new GameObject("SFX_Source");
                sfxObj.transform.SetParent(transform);
                _sfxSource = sfxObj.AddComponent<AudioSource>();
                _sfxSource.playOnAwake = false;
            }

            // Create Music source if not assigned
            if (_musicSource == null)
            {
                GameObject musicObj = new GameObject("Music_Source");
                musicObj.transform.SetParent(transform);
                _musicSource = musicObj.AddComponent<AudioSource>();
                _musicSource.playOnAwake = false;
                _musicSource.loop = true;
            }

            // Create Ambience source if not assigned
            if (_ambienceSource == null)
            {
                GameObject ambObj = new GameObject("Ambience_Source");
                ambObj.transform.SetParent(transform);
                _ambienceSource = ambObj.AddComponent<AudioSource>();
                _ambienceSource.playOnAwake = false;
                _ambienceSource.loop = true;
            }
        }

        private void BuildClipDictionary()
        {
            _sfxClips = new Dictionary<SFXType, AudioClip>
            {
                { SFXType.ShowStart, _showStartClip },
                { SFXType.ShowEnd, _showEndClip },
                { SFXType.CallerIncoming, _callerIncomingClip },
                { SFXType.CallerApproved, _callerApprovedClip },
                { SFXType.CallerRejected, _callerRejectedClip },
                { SFXType.CallerOnAir, _callerOnAirClip },
                { SFXType.CallerComplete, _callerCompleteClip },
                { SFXType.CallerDisconnect, _callerDisconnectClip },
                { SFXType.ButtonClick, _buttonClickClip },
                { SFXType.ItemUsed, _itemUsedClip },
                { SFXType.ItemEmpty, _itemEmptyClip },
                { SFXType.LowStat, _lowStatClip },
                { SFXType.HighListeners, _highListenersClip },
                { SFXType.PhoneRing, _phoneRingClip },
                { SFXType.StaticBurst, _staticBurstClip }
            };
        }

        private void SubscribeToEvents()
        {
            _gameState = GameStateManager.Instance;
            _callerQueue = CallerQueue.Instance;
            _listenerManager = ListenerManager.Instance;
            _itemManager = ItemManager.Instance;

            if (_gameState != null)
            {
                _gameState.OnPhaseChanged += HandlePhaseChanged;
            }

            if (_callerQueue != null)
            {
                _callerQueue.OnCallerAdded += HandleCallerAdded;
                _callerQueue.OnCallerOnAir += HandleCallerOnAir;
                _callerQueue.OnCallerCompleted += HandleCallerCompleted;
                _callerQueue.OnCallerDisconnected += HandleCallerDisconnected;
            }

            if (_listenerManager != null)
            {
                _listenerManager.OnPeakReached += HandlePeakReached;
            }

            if (_itemManager != null)
            {
                _itemManager.OnItemUsed += HandleItemUsed;
            }

            Debug.Log("AudioManager: Subscribed to game events");
        }

        private void UnsubscribeFromEvents()
        {
            if (_gameState != null)
            {
                _gameState.OnPhaseChanged -= HandlePhaseChanged;
            }

            if (_callerQueue != null)
            {
                _callerQueue.OnCallerAdded -= HandleCallerAdded;
                _callerQueue.OnCallerOnAir -= HandleCallerOnAir;
                _callerQueue.OnCallerCompleted -= HandleCallerCompleted;
                _callerQueue.OnCallerDisconnected -= HandleCallerDisconnected;
            }

            if (_listenerManager != null)
            {
                _listenerManager.OnPeakReached -= HandlePeakReached;
            }

            if (_itemManager != null)
            {
                _itemManager.OnItemUsed -= HandleItemUsed;
            }
        }

        #region Event Handlers

        private void HandlePhaseChanged(GamePhase oldPhase, GamePhase newPhase)
        {
            if (newPhase == GamePhase.LiveShow)
            {
                PlaySFX(SFXType.ShowStart);
            }
            else if (oldPhase == GamePhase.LiveShow)
            {
                PlaySFX(SFXType.ShowEnd);
            }
        }

        private void HandleCallerAdded(Caller caller)
        {
            PlaySFX(SFXType.CallerIncoming);
        }

        private void HandleCallerOnAir(Caller caller)
        {
            PlaySFX(SFXType.CallerOnAir);
        }

        private void HandleCallerCompleted(Caller caller)
        {
            PlaySFX(SFXType.CallerComplete);
        }

        private void HandleCallerDisconnected(Caller caller)
        {
            PlaySFX(SFXType.CallerDisconnect);
        }

        private void HandlePeakReached(int newPeak)
        {
            PlaySFX(SFXType.HighListeners);
        }

        private void HandleItemUsed(Data.StatModifier item, int remaining)
        {
            PlaySFX(SFXType.ItemUsed);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Play a sound effect by type.
        /// </summary>
        public void PlaySFX(SFXType sfxType)
        {
            if (_sfxClips.TryGetValue(sfxType, out AudioClip clip) && clip != null)
            {
                float volume = _sfxVolume * _masterVolume;
                _sfxSource.PlayOneShot(clip, volume);
                Debug.Log($"AudioManager: Playing SFX {sfxType}");
            }
            else
            {
                // No clip assigned - log for debugging but don't spam
                Debug.Log($"AudioManager: No clip for SFX {sfxType} (placeholder)");
            }
        }

        /// <summary>
        /// Play a sound effect with custom pitch variation.
        /// </summary>
        public void PlaySFX(SFXType sfxType, float pitchVariation)
        {
            if (_sfxClips.TryGetValue(sfxType, out AudioClip clip) && clip != null)
            {
                float originalPitch = _sfxSource.pitch;
                _sfxSource.pitch = 1f + UnityEngine.Random.Range(-pitchVariation, pitchVariation);
                
                float volume = _sfxVolume * _masterVolume;
                _sfxSource.PlayOneShot(clip, volume);
                
                _sfxSource.pitch = originalPitch;
            }
        }

        /// <summary>
        /// Play caller approved/rejected based on result.
        /// </summary>
        public void PlayCallerDecision(bool approved)
        {
            PlaySFX(approved ? SFXType.CallerApproved : SFXType.CallerRejected);
        }

        /// <summary>
        /// Play button click feedback.
        /// </summary>
        public void PlayButtonClick()
        {
            PlaySFX(SFXType.ButtonClick);
        }

        /// <summary>
        /// Play item empty error sound.
        /// </summary>
        public void PlayItemEmpty()
        {
            PlaySFX(SFXType.ItemEmpty);
        }

        /// <summary>
        /// Play a one-shot static burst.
        /// </summary>
        public void PlayStaticBurst()
        {
            PlaySFX(SFXType.StaticBurst);
        }

        #endregion
    }
}
