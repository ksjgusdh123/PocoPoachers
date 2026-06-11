using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    private const string PREF_MASTER_VOLUME = "Settings.MasterVolume";
    private const string PREF_BGM_VOLUME    = "Settings.BgmVolume";
    private const string PREF_SFX_VOLUME    = "Settings.SfxVolume";

    private AudioSource _bgmSource;
    private AudioSource _sfxSource;

    public float MasterVolume { get; private set; }
    public float BgmVolume { get; private set; }
    public float SfxVolume { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.playOnAwake = false;

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;

        MasterVolume = PlayerPrefs.GetFloat(PREF_MASTER_VOLUME, 1f);
        BgmVolume    = PlayerPrefs.GetFloat(PREF_BGM_VOLUME, 1f);
        SfxVolume    = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 1f);
        ApplyBgmVolume();
    }

    public void PlayBgm(AudioClip clip)
    {
        if (clip == null) return;
        if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

        _bgmSource.clip = clip;
        _bgmSource.Play();
    }

    public void StopBgm() => _bgmSource.Stop();

    public void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip, MasterVolume * SfxVolume);
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(PREF_MASTER_VOLUME, MasterVolume);
        ApplyBgmVolume();
    }

    public void SetBgmVolume(float value)
    {
        BgmVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(PREF_BGM_VOLUME, BgmVolume);
        ApplyBgmVolume();
    }

    public void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(PREF_SFX_VOLUME, SfxVolume);
    }

    private void ApplyBgmVolume() => _bgmSource.volume = MasterVolume * BgmVolume;
}
