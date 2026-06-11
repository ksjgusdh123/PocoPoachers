using UnityEngine;
using UnityEngine.UI;

namespace UltraForgeStudio.Audio
{
    /// <summary>
    /// Syncs UI sliders with an AudioSource, respecting each slider's MinValue/MaxValue.
    /// - Volume: maps audioSource.volume (0..1) into slider min..max
    /// - Pitch:  maps audioSource.pitch (pitchMin..pitchMax) into slider min..max
    /// Uses SetValueWithoutNotify() to avoid feedback loops.
    /// </summary>
    [DisallowMultipleComponent]
    public class UltraForgeAudioSourceSliderSync : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Slider pitchSlider;

        [Header("Pitch Range (AudioSource domain)")]
        [Tooltip("Minimum pitch value used for mapping (e.g. 0.8).")]
        [SerializeField] private float pitchMin = 0.8f;

        [Tooltip("Maximum pitch value used for mapping (e.g. 1.2).")]
        [SerializeField] private float pitchMax = 1.2f;

        [Header("Update")]
        [Tooltip("How often to poll the AudioSource (seconds). 0 = every frame.")]
        [SerializeField] private float updateInterval = 0.05f;

        [Tooltip("Ignore tiny changes to avoid jitter.")]
        [SerializeField] private float volumeEpsilon = 0.001f;

        [Tooltip("Ignore tiny changes to avoid jitter.")]
        [SerializeField] private float pitchEpsilon = 0.001f;

        private float _nextUpdateTime;
        private float _lastVolume = -999f;
        private float _lastPitch = -999f;

        private void Reset()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            SyncNow(force: true);
        }

        private void OnEnable()
        {
            SyncNow(force: true);
        }

        private void Update()
        {
            if (audioSource == null) return;

            if (updateInterval > 0f && Time.unscaledTime < _nextUpdateTime)
                return;

            _nextUpdateTime = Time.unscaledTime + updateInterval;
            SyncNow(force: false);
        }

        public void SyncNow() => SyncNow(force: true);

        private void SyncNow(bool force)
        {
            if (audioSource == null) return;

            // ----- VOLUME (AudioSource 0..1 -> Slider min..max) -----
            if (volumeSlider != null)
            {
                float v = Mathf.Clamp01(audioSource.volume);

                if (force || Mathf.Abs(v - _lastVolume) > volumeEpsilon)
                {
                    float sliderValue = Map01ToSliderRange(volumeSlider, v);
                    volumeSlider.SetValueWithoutNotify(sliderValue);
                    _lastVolume = v;
                }
            }

            // ----- PITCH (AudioSource pitchMin..pitchMax -> Slider min..max) -----
            if (pitchSlider != null)
            {
                float p = audioSource.pitch;

                float min = pitchMin;
                float max = pitchMax;

                // Guarantee valid range
                if (max <= min + 0.0001f)
                {
                    // Fallback safe range
                    min = 0.8f;
                    max = 1.2f;
                }

                float pClamped = Mathf.Clamp(p, min, max);

                if (force || Mathf.Abs(pClamped - _lastPitch) > pitchEpsilon)
                {
                    float t01 = Mathf.InverseLerp(min, max, pClamped);         // 0..1
                    float sliderValue = Map01ToSliderRange(pitchSlider, t01);  // slider min..max
                    pitchSlider.SetValueWithoutNotify(sliderValue);
                    _lastPitch = pClamped;
                }
            }
        }

        private static float Map01ToSliderRange(Slider slider, float t01)
        {
            float min = slider.minValue;
            float max = slider.maxValue;
            return Mathf.Lerp(min, max, Mathf.Clamp01(t01));
        }

        /// <summary>
        /// Converts a slider value (min..max) to a normalized 0..1.
        /// Useful if you want your Player script to use the SAME mapping.
        /// </summary>
        public static float SliderRangeTo01(Slider slider, float sliderValue)
        {
            float min = slider.minValue;
            float max = slider.maxValue;
            if (max <= min + 0.0001f) return 0f;
            return Mathf.InverseLerp(min, max, sliderValue);
        }

        // Optional runtime helpers
        public void SetAudioSource(AudioSource newSource, bool forceSync = true)
        {
            audioSource = newSource;
            _lastVolume = -999f;
            _lastPitch = -999f;
            if (forceSync) SyncNow(force: true);
        }

        public void SetPitchRange(float newMin, float newMax, bool forceSync = true)
        {
            pitchMin = newMin;
            pitchMax = newMax;
            _lastPitch = -999f;
            if (forceSync) SyncNow(force: true);
        }
    }
}