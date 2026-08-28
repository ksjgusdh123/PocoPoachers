using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : Singleton<SoundManager>
{
    private const string BGM_TITLE = "bgm_main";
    private const string BGM_SHELTER = "bgm_shelter";

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
    private AudioSource _panelSource;
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

        _panelSource = gameObject.AddComponent<AudioSource>();
        _panelSource.playOnAwake = false;

        _pitchedSource = gameObject.AddComponent<AudioSource>();
        _pitchedSource.playOnAwake = false;

        MasterVolume = PlayerPrefs.GetFloat(PREF_MASTER_VOLUME, 1f);
        BgmVolume    = PlayerPrefs.GetFloat(PREF_BGM_VOLUME, 1f);
        SfxVolume    = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 1f);
        ApplyBgmVolume();

        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplySceneBgm(SceneManager.GetActiveScene().name); // 쉘터 씬에서 바로 플레이할 때도 깔리도록
    }

    protected override void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        base.OnDestroy();
    }

    // 씬 진입 시 BGM을 자동으로 갈아끼운다. 같은 곡이면 PlayBgmClip이 막아주므로 다시 시작되지 않는다.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplySceneBgm(scene.name);

    private void ApplySceneBgm(string sceneName)
    {
        // 캐릭터 생성은 타이틀 곡을 그대로 이어 둔다 — 타이틀에서 곧장 넘어가는 화면이라 곡이 끊기면 거슬린다.
        // 로딩 화면은 예외로 두지 않는다. 쉘터 곡이 레이드 로딩까지 따라 들어간다.
        if (sceneName == SceneName.CharacterCreate) return;

        string key = GetSceneBgmKey(sceneName);
        if (string.IsNullOrEmpty(key))
            StopBgm();
        else
            PlayBgm(key);
    }

    // 전용 BGM이 없는 씬(레이드·튜토리얼·결과)은 null — 쉘터 곡이 따라 들어가지 않게 무음으로 둔다
    private static string GetSceneBgmKey(string sceneName)
    {
        if (sceneName == SceneName.Title) return BGM_TITLE;
        if (SceneName.IsShelter(sceneName)) return BGM_SHELTER;
        return null;
    }

    // 싱글톤이 지연 생성이라, 아무도 소리를 내지 않으면 씬 BGM도 깔리지 않는다. 시작 시 한 번 깨워둔다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists() => GetInstance();

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

    public void PlaySfx(string key) => PlaySfx(key, 1f);

    // volumeScale은 sound.csv의 volume에 곱해진다. 같은 소리를 상황에 따라 줄여 낼 때 쓴다
    // (예: 내 발소리는 2D라 또렷해서 팀원 발소리보다 크게 들린다).
    public void PlaySfx(string key, float volumeScale)
    {
        if (string.IsNullOrEmpty(key)) return;

        var data = SoundTable.Instance.Get(key);
        if (data == null || string.IsNullOrEmpty(data.Path)) return;

        PlaySfxClip(ResourceManager.GetInstance().Load<AudioClip>(data.Path), data.Volume * volumeScale);
    }

    // 효과음 길이(초). 클립을 못 찾으면 0.
    // 두 효과음을 끊김 없이 이어 재생할 때 다음 소리의 시작 시점을 잡는 데 쓴다.
    public float GetSfxLength(string key)
    {
        if (string.IsNullOrEmpty(key)) return 0f;

        var data = SoundTable.Instance.Get(key);
        if (data == null || string.IsNullOrEmpty(data.Path)) return 0f;

        var clip = ResourceManager.GetInstance().Load<AudioClip>(data.Path);
        return clip != null ? clip.length : 0f;
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

    // 호송 포드처럼 소리를 내며 움직이는 대상. PlaySfxAt은 재생 시점 위치에 소리를 묶어두므로 이동을 못 따라간다.
    // 대상 밑에 붙인 임시 소스로 재생하고 클립이 끝나면 스스로 사라진다 — 대상이 먼저 파괴되면 소리도 함께 끊긴다.
    public void PlaySfxOn(string key, Transform parent, float maxDistance = 0f)
    {
        if (string.IsNullOrEmpty(key) || parent == null) return;

        var data = SoundTable.Instance.Get(key);
        if (data == null || string.IsNullOrEmpty(data.Path)) return;

        var clip = ResourceManager.GetInstance().Load<AudioClip>(data.Path);
        if (clip == null) return;

        var go = new GameObject($"Sfx3D_{key}");
        go.transform.SetParent(parent, false);

        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = SFX_3D_MIN_DISTANCE;
        source.maxDistance = maxDistance > 0f ? maxDistance : SFX_3D_DEFAULT_MAX_DISTANCE;
        source.dopplerLevel = 0f;
        source.clip = clip;
        source.volume = MasterVolume * SfxVolume * data.Volume;
        source.Play();

        Destroy(go, clip.length);
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

    // 패널 여닫음 소리. 열림 소리가 다 울리기 전에 창을 닫으면 끊어야 해서 전용 소스를 쓴다.
    // 아이템 사용음(_cancelableSource)과 나눠 둔 건, 한 소스를 공유하면 창을 여닫을 때마다
    // 사용 중인 아이템 소리가 끊기기 때문이다.
    // 실제로 재생했으면 true — 호출측이 지금 울리는 소리의 주인을 추적하는 데 쓴다.
    public bool PlayPanelSfx(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;

        var data = SoundTable.Instance.Get(key);
        if (data == null || string.IsNullOrEmpty(data.Path)) return false;

        var clip = ResourceManager.GetInstance().Load<AudioClip>(data.Path);
        if (clip == null) return false;

        _panelSource.clip = clip;
        _panelSource.volume = MasterVolume * SfxVolume * data.Volume;
        _panelSource.Play();
        return true;
    }

    public void StopPanelSfx() => _panelSource.Stop();

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
