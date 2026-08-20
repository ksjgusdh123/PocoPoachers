using System.Collections.Generic;
using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    private const string PREF_MASTER_VOLUME = "Settings.MasterVolume";
    private const string PREF_BGM_VOLUME    = "Settings.BgmVolume";
    private const string PREF_SFX_VOLUME    = "Settings.SfxVolume";

    private const string CLICK_SFX_KEY = "ui_click";

    private const int SFX_3D_MAX_SOURCES = 24;
    // 리스너가 RaidCamera에 붙어 있고 카메라 오프셋이 (0,10,-7)이라, 발밑에서 난 소리도 리스너와는
    // 12m쯤 떨어져 있다. minDistance를 그만큼 확보해야 가까운 총성이 감쇠로 먹히지 않는다.
    private const float SFX_3D_MIN_DISTANCE = 12f;
    private const float SFX_3D_DEFAULT_MAX_DISTANCE = 30f;

    private AudioSource _bgmSource;
    private AudioSource _sfxSource;
    private AudioSource _cancelableSource;
    private AudioSource _pitchedSource;
    private readonly List<AudioSource> _sfx3DSources = new List<AudioSource>();
    private int _next3DSourceIndex;
    private float _bgmClipVolume = 1f;

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

        _cancelableSource = gameObject.AddComponent<AudioSource>();
        _cancelableSource.playOnAwake = false;

        _pitchedSource = gameObject.AddComponent<AudioSource>();
        _pitchedSource.playOnAwake = false;

        MasterVolume = PlayerPrefs.GetFloat(PREF_MASTER_VOLUME, 1f);
        BgmVolume    = PlayerPrefs.GetFloat(PREF_BGM_VOLUME, 1f);
        SfxVolume    = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 1f);
        ApplyBgmVolume();
    }

    public void PlayBgm(string key)
    {
        if (string.IsNullOrEmpty(key)) return;

        var data = SoundTable.Instance.Get(key);
        if (data == null || string.IsNullOrEmpty(data.Path)) return;

        _bgmClipVolume = data.Volume;
        ApplyBgmVolume();
        PlayBgmClip(ResourceManager.GetInstance().Load<AudioClip>(data.Path));
    }

    public void StopBgm() => _bgmSource.Stop();

    public void PlaySfx(string key)
    {
        if (string.IsNullOrEmpty(key)) return;

        var data = SoundTable.Instance.Get(key);
        if (data == null || string.IsNullOrEmpty(data.Path)) return;

        PlaySfxClip(ResourceManager.GetInstance().Load<AudioClip>(data.Path), data.Volume);
    }

    // 총성처럼 발생 위치가 있는 효과음. 2D PlaySfx로 재생하면 맵 반대편 총성이 귀 옆에서 울린다.
    public void PlaySfxAt(string key, Vector3 position, float maxDistance = 0f)
    {
        if (string.IsNullOrEmpty(key)) return;

        var data = SoundTable.Instance.Get(key);
        if (data == null || string.IsNullOrEmpty(data.Path)) return;

        var clip = ResourceManager.GetInstance().Load<AudioClip>(data.Path);
        if (clip == null) return;

        AudioSource source = Get3DSource();
        source.transform.position = position;
        source.maxDistance = maxDistance > 0f ? maxDistance : SFX_3D_DEFAULT_MAX_DISTANCE;
        source.volume = MasterVolume * SfxVolume * data.Volume;
        source.PlayOneShot(clip);
    }

    // 도중에 멈춰야 하는 2D 효과음(아이템 사용 등). PlayOneShot은 정지할 수 없어 전용 소스를 쓴다.
    // 동시에 하나만 재생되며, 새로 재생하면 이전 것은 끊긴다.
    public void PlayCancelableSfx(string key)
    {
        if (string.IsNullOrEmpty(key)) return;

        var data = SoundTable.Instance.Get(key);
        if (data == null || string.IsNullOrEmpty(data.Path)) return;

        var clip = ResourceManager.GetInstance().Load<AudioClip>(data.Path);
        if (clip == null) return;

        _cancelableSource.clip = clip;
        _cancelableSource.volume = MasterVolume * SfxVolume * data.Volume;
        _cancelableSource.Play();
    }

    public void StopCancelableSfx() => _cancelableSource.Stop();

    // 대사 blip처럼 짧은 소리를 연달아 낼 때 쓴다. 매번 같은 피치로 반복하면 말소리가 아니라
    // 기계음으로 들리므로 재생마다 피치를 조금씩 흔든다. 공용 _sfxSource의 피치를 건드리면
    // 다른 UI 소리까지 변조되므로 전용 소스를 쓴다.
    public void PlaySfxPitched(string key, float pitchVariance)
    {
        if (string.IsNullOrEmpty(key)) return;

        var data = SoundTable.Instance.Get(key);
        if (data == null || string.IsNullOrEmpty(data.Path)) return;

        var clip = ResourceManager.GetInstance().Load<AudioClip>(data.Path);
        if (clip == null) return;

        _pitchedSource.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
        _pitchedSource.PlayOneShot(clip, MasterVolume * SfxVolume * data.Volume);
    }

    public void PlayButtonClick() => PlaySfx(CLICK_SFX_KEY);

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

    private void PlayBgmClip(AudioClip clip)
    {
        if (clip == null) return;
        if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

        _bgmSource.clip = clip;
        _bgmSource.Play();
    }

    private void PlaySfxClip(AudioClip clip, float clipVolume)
    {
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip, MasterVolume * SfxVolume * clipVolume);
    }

    // 재생이 끝난 소스를 우선 재사용하고, 상한에 닿으면 가장 오래 쓴 것을 뺏는다.
    // 최대 720RPM 연사 중에는 소스가 모자랄 수 있는데, 그때는 무음보다 겹쳐 끊기는 편이 낫다.
    private AudioSource Get3DSource()
    {
        for (int i = 0; i < _sfx3DSources.Count; i++)
            if (!_sfx3DSources[i].isPlaying) return _sfx3DSources[i];

        if (_sfx3DSources.Count < SFX_3D_MAX_SOURCES)
        {
            var go = new GameObject($"Sfx3D_{_sfx3DSources.Count}");
            go.transform.SetParent(transform);

            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = SFX_3D_MIN_DISTANCE;
            source.dopplerLevel = 0f;
            _sfx3DSources.Add(source);
            return source;
        }

        AudioSource oldest = _sfx3DSources[_next3DSourceIndex];
        _next3DSourceIndex = (_next3DSourceIndex + 1) % _sfx3DSources.Count;
        oldest.Stop();
        return oldest;
    }

    private void ApplyBgmVolume() => _bgmSource.volume = MasterVolume * BgmVolume * _bgmClipVolume;
}
