using System;
using UnityEngine;
namespace UltraForgeStudio.Audio.Music
{
    [DisallowMultipleComponent]


    public class UltraForgeMusicPlayer : MonoBehaviour
    {
        public enum PlaybackMode
        {
            PlayOnlyOnPlayButton,
            AutoPlayFirstTrack,
            AutoPlayAllTracks,
            AutoPlayRandomTracks
        }
        [Header("Pitch Settings")]
        [SerializeField, Range(0.5f, 2f)] private float pitch = 1f;
        [SerializeField] private float pitchMin = 0.33f;
        [SerializeField] private float pitchMax = 3f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] playlist;

        [Header("Playback Mode")]
        [SerializeField] private PlaybackMode playbackMode = PlaybackMode.AutoPlayAllTracks;

        [Header("Settings")]
        [SerializeField] private bool loopSingleTrack = false;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        [Header("State (Read Only)")]
        [SerializeField] private int currentIndex = 0;

        public event Action<string, int, int> OnTrackChanged;
        public event Action<bool> OnPlayStateChanged;

        private bool hasAutoPlayed;

        private void Reset()
        {
            audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

        private void Awake()
        {
            audioSource.pitch = pitch;
            audioSource.loop = false;
            audioSource.volume = volume;
            audioSource.pitch = pitch;

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.volume = volume;

            if (HasTracks())
            {
                currentIndex = Mathf.Clamp(currentIndex, 0, playlist.Length - 1);
                ApplyClip(currentIndex);
            }
        }

        private void Start()
        {
            HandleAutoPlay();
        }

        private void Update()
        {
            if (!audioSource.isPlaying || !HasTracks() || audioSource.clip == null)
                return;

            if (audioSource.time >= audioSource.clip.length - 0.05f)
            {
                HandleTrackFinished();
            }
        }

        // =======================
        // PUBLIC CONTROLS
        // =======================

        public void Play()
        {
            if (!HasTracks()) return;

            if (audioSource.clip == null)
                ApplyClip(currentIndex);

            audioSource.Play();
            OnPlayStateChanged?.Invoke(true);
        }

        public void Pause()
        {
            audioSource.Pause();
            OnPlayStateChanged?.Invoke(false);
        }

        public void Stop()
        {
            audioSource.Stop();
            audioSource.time = 0f;
            OnPlayStateChanged?.Invoke(false);
        }

        public void Next()
        {
            if (!HasTracks()) return;
            SetTrackIndex((currentIndex + 1) % playlist.Length, true);
        }

        public void Previous()
        {
            if (!HasTracks()) return;
            int prev = currentIndex - 1;
            if (prev < 0) prev = playlist.Length - 1;
            SetTrackIndex(prev, true);
        }

        public void SetTrackIndex(int index, bool autoPlay)
        {
            if (!HasTracks()) return;

            currentIndex = Mathf.Clamp(index, 0, playlist.Length - 1);
            ApplyClip(currentIndex);

            if (autoPlay)
                Play();
        }

        public void SetVolume(float v)
        {
            volume = Mathf.Clamp01(v);
            if (audioSource != null)
                audioSource.volume = volume;
        }
        // =======================
        // UI SLIDER CONTROLS
        // =======================

        /// <summary>
        /// Use with a UI Slider (0–1).
        /// </summary>
        public void SetVolumeFromSlider(float value)
        {
            volume = Mathf.Clamp01(value);
            if (audioSource != null)
                audioSource.volume = volume;
        }

        /// <summary>
        /// Use with a UI Slider (0–1).
        /// Maps slider value to pitchMin–pitchMax.
        /// </summary>
        public void SetPitchFromSlider(float value)
        {

            if (audioSource != null)
                audioSource.pitch = 1;
        }

        // =======================
        // UI-FRIENDLY FUNCTIONS
        // =======================

        // Playback mode buttons
        public void SetMode_PlayOnlyOnPlayButton()
        {
            SetPlaybackMode(PlaybackMode.PlayOnlyOnPlayButton);
        }

        public void SetMode_AutoPlayFirstTrack()
        {
            SetPlaybackMode(PlaybackMode.AutoPlayFirstTrack);
        }

        public void SetMode_AutoPlayAllTracks()
        {
            SetPlaybackMode(PlaybackMode.AutoPlayAllTracks);
        }

        public void SetMode_AutoPlayRandomTracks()
        {
            SetPlaybackMode(PlaybackMode.AutoPlayRandomTracks);
        }

        /// <summary>
        /// Generic setter if you want to call it from code.
        /// (UI Buttons can't pass enums easily, hence the 4 wrapper methods above.)
        /// </summary>
        public void SetPlaybackMode(PlaybackMode mode)
        {
            playbackMode = mode;
            hasAutoPlayed = false; // allow AutoPlayFirstTrack to trigger again if selected

            // Optional: If switching into an auto-play mode and we're not playing, start.
            if (!audioSource.isPlaying && HasTracks())
            {
                if (playbackMode == PlaybackMode.AutoPlayAllTracks ||
                    playbackMode == PlaybackMode.AutoPlayRandomTracks ||
                    playbackMode == PlaybackMode.AutoPlayFirstTrack)
                {
                    HandleAutoPlay();
                }
            }
        }

        // Loop single track toggle
        public void SetLoopSingleTrack(bool isOn)
        {
            loopSingleTrack = isOn;
        }

        public void ToggleLoopSingleTrack()
        {
            loopSingleTrack = !loopSingleTrack;
        }

        // Random track button
        public void PlayRandomTrackNow()
        {
            if (!HasTracks()) return;

            int randomIndex = currentIndex;

            if (playlist.Length > 1)
            {
                do
                {
                    randomIndex = UnityEngine.Random.Range(0, playlist.Length);
                }
                while (randomIndex == currentIndex);
            }

            SetTrackIndex(randomIndex, true);
        }

        // =======================
        // INTERNAL LOGIC
        // =======================

        private void HandleAutoPlay()
        {
            if (!HasTracks()) return;

            switch (playbackMode)
            {
                case PlaybackMode.PlayOnlyOnPlayButton:
                    break;

                case PlaybackMode.AutoPlayFirstTrack:
                    if (!hasAutoPlayed)
                    {
                        hasAutoPlayed = true;
                        // Ensure we start from first track for this mode
                        currentIndex = 0;
                        ApplyClip(currentIndex);
                        Play();
                    }
                    break;

                case PlaybackMode.AutoPlayAllTracks:
                case PlaybackMode.AutoPlayRandomTracks:
                    Play();
                    break;
            }
        }

        private void HandleTrackFinished()
        {
            if (loopSingleTrack)
            {
                audioSource.time = 0f;
                audioSource.Play();
                return;
            }

            switch (playbackMode)
            {
                case PlaybackMode.PlayOnlyOnPlayButton:
                case PlaybackMode.AutoPlayFirstTrack:
                    Stop();
                    break;

                case PlaybackMode.AutoPlayAllTracks:
                    Next();
                    break;

                case PlaybackMode.AutoPlayRandomTracks:
                    PlayRandomTrack_Internal();
                    break;
            }
        }

        private void PlayRandomTrack_Internal()
        {
            if (playlist.Length <= 1) return;

            int randomIndex;
            do
            {
                randomIndex = UnityEngine.Random.Range(0, playlist.Length);
            }
            while (randomIndex == currentIndex);

            SetTrackIndex(randomIndex, true);
        }

        private void ApplyClip(int index)
        {
            if (!HasTracks()) return;

            audioSource.clip = playlist[index];
            audioSource.time = 0f;
            if (audioSource.clip != null)
                OnTrackChanged?.Invoke(audioSource.clip.name, index, playlist.Length);
        }

        private bool HasTracks()
        {
            return playlist != null && playlist.Length > 0;
        }
    }
}