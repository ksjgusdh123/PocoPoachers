using UnityEngine;
using UnityEngine.UI;

// ── Inspector 연결 구조 ──────────────────────────────────────────────────
//  OptionsUI   [Canvas / CanvasGroup]
//  ├── Dimmer            [Image]
//  └── Panel
//      ├── Slider_Master [Slider]
//      ├── Slider_Bgm    [Slider]
//      ├── Slider_Sfx    [Slider]
//      └── Btn_Close     [Button]
// ────────────────────────────────────────────────────────────────────────

public class OptionsUI : UIBase
{
    [Header("Volume Sliders")]
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _sfxSlider;

    [Header("Buttons")]
    [SerializeField] private Button _btnClose;

    protected override UIType UiType => UIType.Options;

    protected override void Awake()
    {
        base.Awake();

        var sound = SoundManager.GetInstance();

        _masterSlider.SetValueWithoutNotify(sound.MasterVolume);
        _bgmSlider.SetValueWithoutNotify(sound.BgmVolume);
        _sfxSlider.SetValueWithoutNotify(sound.SfxVolume);

        _masterSlider.onValueChanged.AddListener(sound.SetMasterVolume);
        _bgmSlider.onValueChanged.AddListener(sound.SetBgmVolume);
        _sfxSlider.onValueChanged.AddListener(sound.SetSfxVolume);

        _btnClose.onClick.AddListener(Hide);
    }
}
