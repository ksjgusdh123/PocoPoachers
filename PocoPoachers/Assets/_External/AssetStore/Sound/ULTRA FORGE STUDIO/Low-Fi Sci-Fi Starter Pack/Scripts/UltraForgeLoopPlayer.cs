using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UltraForgeStudio.Audio.Music
{
    [DisallowMultipleComponent]
    public class UltraForgeLoopPlayer : MonoBehaviour
    {
        public enum PlaybackMode
        {
            PlayOnlyOnPlayButton,
            AutoPlayFirstTrack,
            AutoPlayAllTracks,
            AutoPlayRandomTracks
        }

        [Serializable]
        public class LoopEntry
        {
            [Tooltip("Audio clip for this loop variation.")]
            public AudioClip clip;

            [Header("Per-Loop Randomization (optional)")]
            [Range(0f, 1f)] public float volumeMin = 0.9f;
            [Range(0f, 1f)] public float volumeMax = 1.0f;

            [Range(0.33f, 3f)] public float pitchMin = 0.98f;
            [Range(0.33f, 3f)] public float pitchMax = 1.02f;

            [Header("Repeat Settings (per-loop defaults)")]
            [Min(1)] public int repeat = 4;

            public bool randomRepeat = false;

            [Min(1)] public int repeatMin = 2;
            [Min(1)] public int repeatMax = 6;

            public int GetRepeatCount(System.Random rng)
            {
                if (!randomRepeat) return Mathf.Max(1, repeat);
                int min = Mathf.Max(1, repeatMin);
                int max = Mathf.Max(min, repeatMax);
                return rng.Next(min, max + 1);
            }

            public float GetRandomVolume()
            {
                float min = Mathf.Clamp01(Mathf.Min(volumeMin, volumeMax));
                float max = Mathf.Clamp01(Mathf.Max(volumeMin, volumeMax));
                return UnityEngine.Random.Range(min, max);
            }

            public float GetRandomPitch()
            {
                float min = Mathf.Min(pitchMin, pitchMax);
                float max = Mathf.Max(pitchMin, pitchMax);
                return UnityEngine.Random.Range(min, max);
            }
        }

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private LoopEntry[] loops;

        [Header("Playback Mode")]
        [SerializeField] private PlaybackMode playbackMode = PlaybackMode.AutoPlayAllTracks;

        [Header("Loop Selection")]
        [Tooltip("If ON, selects next loop randomly when advancing. If OFF, sequential.")]
        [SerializeField] private bool randomizeLoops = true;

        [Tooltip("Avoid selecting the same loop twice in a row (only applies when randomizeLoops is ON).")]
        [SerializeField] private bool avoidImmediateRepeat = true;

        [Header("Repeat Control (global override)")]
        [Tooltip("If ON, overrides each LoopEntry repeat settings with the values below.")]
        [SerializeField] private bool useGlobalRepeatSettings = true;

        [Tooltip("If RandomRepeatGlobal is OFF, each selected loop will repeat this many times before switching.")]
        [Min(1)]
        [SerializeField] private int globalRepeat = 4;

        [Tooltip("If ON, repeats are randomized per selected loop using GlobalRepeatMin/Max.")]
        [SerializeField] private bool randomRepeatGlobal = false;

        [Min(1)]
        [SerializeField] private int globalRepeatMin = 2;

        [Min(1)]
        [SerializeField] private int globalRepeatMax = 6;

        [Header("Single Looping")]
        [Tooltip("If ON, repeats the current loop forever (ignores repeat counts).")]
        [SerializeField] private bool loopSingleTrack = false;

        [Header("Global Volume/Pitch (UI sliders)")]
        [Range(0f, 1f)]
        [SerializeField] private float volume = 1f;

        [Range(0.33f, 3f)]
        [SerializeField] private float pitch = 1f;

        [SerializeField] private float pitchMin = 0.33f;
        [SerializeField] private float pitchMax = 3f;

        [Header("Start")]
        [SerializeField] private bool playOnStart = true;

        [Header("State (Read Only)")]
        [SerializeField] private int currentIndex = 0;
        [SerializeField] private int repeatsRemainingForCurrentSelection = 0;

        public event Action<string, int, int> OnTrackChanged; // (clipName, index, total)
        public event Action<bool> OnPlayStateChanged;

        private System.Random _rng;
        private bool hasAutoPlayed;
        private bool _finishHandledThisClip;

        private void Reset()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;

            _rng = new System.Random(Environment.TickCount);

            if (HasTracks())
            {
                currentIndex = Mathf.Clamp(currentIndex, 0, loops.Length - 1);
                ApplyClip(currentIndex, applyPerLoopRandomization: true);
            }

            ApplyVolumeAndPitch();
        }

        private void Start()
        {
            if (playOnStart)
                HandleAutoPlay();
        }

        private void Update()
        {
            if (!HasTracks() || audioSource.clip == null) return;

            if (audioSource.isPlaying)
            {
                _finishHandledThisClip = false;
                return;
            }

            if (_finishHandledThisClip) return;

            // Clip ended this frame
            _finishHandledThisClip = true;
            HandleTrackFinished();
        }

        // =======================
        // PUBLIC CONTROLS (same style as MusicPlayer)
        // =======================

        public void Play()
        {
            if (!HasTracks()) return;

            if (audioSource.clip == null)
                ApplyClip(currentIndex, applyPerLoopRandomization: true);

            audioSource.Play();
            OnPlayStateChanged?.Invoke(true);
        }
        public void Repeat3()
        {
            useGlobalRepeatSettings = true;
            randomRepeatGlobal = false;
            globalRepeat = 3;

            // Optional: apply immediately to the current selection too
            repeatsRemainingForCurrentSelection = 3;
        }
        public void Repeat5()
        {
            useGlobalRepeatSettings = true;
            randomRepeatGlobal = false;
            globalRepeat = 5;

            // Optional: apply immediately to the current selection too
            repeatsRemainingForCurrentSelection = 5;
        }
        public void Repeat10()
        {
            useGlobalRepeatSettings = true;
            randomRepeatGlobal = false;
            globalRepeat = 10;

            // Optional: apply immediately to the current selection too
            repeatsRemainingForCurrentSelection = 10;
        }
        public void RepeatRandom()
        {
            useGlobalRepeatSettings = true;
            randomRepeatGlobal = true;
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
            repeatsRemainingForCurrentSelection = 0;
            OnPlayStateChanged?.Invoke(false);
        }

        public void Next()
        {
            AdvanceToNextTrack(autoPlay: true);
        }

        public void Previous()
        {
            if (!HasTracks()) return;

            int prev = currentIndex - 1;
            if (prev < 0) prev = loops.Length - 1;

            SetTrackIndex(prev, autoPlay: true);
        }

        public void SetTrackIndex(int index, bool autoPlay)
        {
            if (!HasTracks()) return;

            currentIndex = Mathf.Clamp(index, 0, loops.Length - 1);
            PrepareRepeatCountForSelection();
            ApplyClip(currentIndex, applyPerLoopRandomization: true);

            if (autoPlay) Play();
        }

        // Random track button
        public void PlayRandomTrackNow()
        {
            if (!HasTracks()) return;

            int randomIndex = currentIndex;

            if (loops.Length > 1)
            {
                do { randomIndex = UnityEngine.Random.Range(0, loops.Length); }
                while (randomIndex == currentIndex);
            }

            SetTrackIndex(randomIndex, autoPlay: true);
        }

        // Loop single track toggle
        public void SetLoopSingleTrack(bool isOn) => loopSingleTrack = isOn;
        public void ToggleLoopSingleTrack() => loopSingleTrack = !loopSingleTrack;

        // Playback mode buttons
        public void SetMode_PlayOnlyOnPlayButton() => SetPlaybackMode(PlaybackMode.PlayOnlyOnPlayButton);
        public void SetMode_AutoPlayFirstTrack() => SetPlaybackMode(PlaybackMode.AutoPlayFirstTrack);
        public void SetMode_AutoPlayAllTracks() => SetPlaybackMode(PlaybackMode.AutoPlayAllTracks);
        public void SetMode_AutoPlayRandomTracks() => SetPlaybackMode(PlaybackMode.AutoPlayRandomTracks);

        public void SetPlaybackMode(PlaybackMode mode)
        {
            playbackMode = mode;
            hasAutoPlayed = false;

            if (!audioSource.isPlaying && HasTracks())
                HandleAutoPlay();
        }

        // =======================
        // UI SLIDERS (volume / pitch)
        // =======================

        /// <summary>
        /// Slider range: 0..1
        /// </summary>
        public void SetVolumeFromSlider(float value01)
        {
            volume = Mathf.Clamp01(value01);
            ApplyVolumeAndPitch();
        }

        /// <summary>
        /// Slider range: 0..1 mapped to pitchMin..pitchMax
        /// </summary>
        public void SetPitchFromSlider(float value01)
        {
            float t = Mathf.Clamp01(value01);
            pitch = Mathf.Lerp(pitchMin, pitchMax, t);
            ApplyVolumeAndPitch();
        }

        // =======================
        // REPEAT CONTROL (what you asked for)
        // =======================

        /// <summary>
        /// Use a Slider/Dropdown/InputField to set how many times EACH selected loop repeats before switching.
        /// This sets a GLOBAL override (requires UseGlobalRepeatSettings = true).
        /// </summary>
        public void SetGlobalRepeatCount(int repeats)
        {
            globalRepeat = Mathf.Max(1, repeats);
            useGlobalRepeatSettings = true;
            randomRepeatGlobal = false;
        }

        /// <summary>
        /// Use a Slider (Whole Numbers) and pass float -> int.
        /// </summary>
        public void SetGlobalRepeatCountFromSlider(float repeats)
        {
            SetGlobalRepeatCount(Mathf.RoundToInt(repeats));
        }

        /// <summary>
        /// Toggle whether repeat count is randomized per selection (GLOBAL).
        /// </summary>
        public void SetRandomRepeatGlobal(bool isOn)
        {
            randomRepeatGlobal = isOn;
            useGlobalRepeatSettings = true;
        }

        public void ToggleRandomRepeatGlobal()
        {
            SetRandomRepeatGlobal(!randomRepeatGlobal);
        }

        public void SetGlobalRepeatMinMax(int min, int max)
        {
            globalRepeatMin = Mathf.Max(1, min);
            globalRepeatMax = Mathf.Max(globalRepeatMin, max);
            useGlobalRepeatSettings = true;
            randomRepeatGlobal = true;
        }

        public void SetUseGlobalRepeatSettings(bool isOn)
        {
            useGlobalRepeatSettings = isOn;
        }

        // =======================
        // INTERNAL LOGIC
        // =======================

        private bool HasTracks() => loops != null && loops.Length > 0;

        private void HandleAutoPlay()
        {
            if (!HasTracks()) return;

            switch (playbackMode)
            {
                case PlaybackMode.PlayOnlyOnPlayButton:
                    // Do nothing
                    break;

                case PlaybackMode.AutoPlayFirstTrack:
                    if (!hasAutoPlayed)
                    {
                        hasAutoPlayed = true;
                        currentIndex = 0;
                        PrepareRepeatCountForSelection();
                        ApplyClip(currentIndex, applyPerLoopRandomization: true);
                        Play();
                    }
                    break;

                case PlaybackMode.AutoPlayAllTracks:
                case PlaybackMode.AutoPlayRandomTracks:
                    if (audioSource.clip == null)
                    {
                        PrepareRepeatCountForSelection();
                        ApplyClip(currentIndex, applyPerLoopRandomization: true);
                    }
                    Play();
                    break;
            }
        }

        private void HandleTrackFinished()
        {
            if (!HasTracks()) return;

            if (loopSingleTrack)
            {
                // Repeat forever
                audioSource.time = 0f;
                audioSource.Play();
                OnPlayStateChanged?.Invoke(true);
                return;
            }

            // Decrement remaining repeats for this selection.
            // repeatsRemainingForCurrentSelection includes the current play already scheduled.
            if (repeatsRemainingForCurrentSelection > 0)
                repeatsRemainingForCurrentSelection--;

            // If still repeats remain, replay SAME clip.
            if (repeatsRemainingForCurrentSelection > 0)
            {
                // Re-apply per-loop randomization each repeat if you want subtle variation:
                ApplyClip(currentIndex, applyPerLoopRandomization: true);
                Play();
                return;
            }

            // Otherwise advance according to mode.
            switch (playbackMode)
            {
                case PlaybackMode.PlayOnlyOnPlayButton:
                case PlaybackMode.AutoPlayFirstTrack:
                    Stop();
                    break;

                case PlaybackMode.AutoPlayAllTracks:
                    AdvanceToNextTrack(autoPlay: true);
                    break;

                case PlaybackMode.AutoPlayRandomTracks:
                    PlayRandomTrackNow();
                    break;
            }
        }

        private void AdvanceToNextTrack(bool autoPlay)
        {
            if (!HasTracks()) return;

            int nextIndex;

            if (randomizeLoops)
            {
                nextIndex = GetRandomNextIndex();
            }
            else
            {
                nextIndex = (currentIndex + 1) % loops.Length;
            }

            currentIndex = nextIndex;
            PrepareRepeatCountForSelection();
            ApplyClip(currentIndex, applyPerLoopRandomization: true);

            if (autoPlay) Play();
        }

        private int GetRandomNextIndex()
        {
            if (loops.Length == 1) return 0;

            int candidate;
            int safety = 0;
            do
            {
                candidate = _rng.Next(0, loops.Length);
                safety++;
            }
            while (avoidImmediateRepeat && candidate == currentIndex && safety < 50);

            return candidate;
        }

        private void PrepareRepeatCountForSelection()
        {
            // Determines how many times the selected loop should play before switching.
            // This is set at the moment a new loop is selected (not per repeat).
            if (!HasTracks()) return;

            if (useGlobalRepeatSettings)
            {
                if (!randomRepeatGlobal)
                {
                    repeatsRemainingForCurrentSelection = Mathf.Max(1, globalRepeat);
                }
                else
                {
                    int min = Mathf.Max(1, globalRepeatMin);
                    int max = Mathf.Max(min, globalRepeatMax);
                    repeatsRemainingForCurrentSelection = _rng.Next(min, max + 1);
                }

                return;
            }

            // Otherwise use per-loop entry settings
            LoopEntry entry = loops[currentIndex];
            if (entry == null)
            {
                repeatsRemainingForCurrentSelection = 1;
                return;
            }

            repeatsRemainingForCurrentSelection = Mathf.Max(1, entry.GetRepeatCount(_rng));
        }

        private void ApplyClip(int index, bool applyPerLoopRandomization)
        {
            if (!HasTracks()) return;

            LoopEntry entry = loops[index];
            if (entry == null || entry.clip == null)
            {
                // Find next valid clip
                for (int i = 0; i < loops.Length; i++)
                {
                    int idx = (index + i) % loops.Length;
                    if (loops[idx] != null && loops[idx].clip != null)
                    {
                        entry = loops[idx];
                        currentIndex = idx;
                        break;
                    }
                }

                if (entry == null || entry.clip == null)
                {
                    Stop();
                    return;
                }
            }

            audioSource.clip = entry.clip;
            audioSource.time = 0f;

            // Volume/Pitch:
            // - global slider values always apply
            // - optionally multiply by per-loop random ranges for subtle variation
            float v = volume;
            float p = pitch;

            if (applyPerLoopRandomization)
            {
                v *= entry.GetRandomVolume();
                p *= entry.GetRandomPitch();
            }

            audioSource.volume = Mathf.Clamp01(v);
            audioSource.pitch = Mathf.Clamp(p, pitchMin, pitchMax);

            OnTrackChanged?.Invoke(audioSource.clip.name, currentIndex, loops.Length);
        }

        private void ApplyVolumeAndPitch()
        {
            if (audioSource == null) return;

            // Do NOT overwrite per-loop randomization here; this is "global" adjustment.
            // It applies immediately to currently playing audio.
            audioSource.volume = Mathf.Clamp01(volume);
            audioSource.pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        }
    }
}