using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ── Inspector 연결 구조 ──────────────────────────────────────────────────
//  OptionsUI   [Canvas / CanvasGroup]
//  ├── Dimmer            [Image]
//  └── Panel
//      ├── Slider_Master     [Slider]
//      ├── Slider_Bgm        [Slider]
//      ├── Slider_Sfx        [Slider]
//      ├── Dropdown_Language [TMP_Dropdown]
//      └── Btn_Close         [Button]
// ────────────────────────────────────────────────────────────────────────

public class OptionsUI : UIBase
{
    private static readonly Dictionary<SystemLanguage, string> LanguageDisplayNames = new()
    {
        { SystemLanguage.Korean, "한국어" },
        { SystemLanguage.English, "English" },
    };

    [Header("Volume Sliders")]
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _sfxSlider;

    [Header("Language")]
    [SerializeField] private TMP_Dropdown _languageDropdown;

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

        var localization = LocalizationManager.GetInstance();

        var languageOptions = new List<string>();
        foreach (var language in LocalizationManager.SupportedLanguages)
            languageOptions.Add(LanguageDisplayNames.TryGetValue(language, out var name) ? name : language.ToString());

        _languageDropdown.ClearOptions();
        _languageDropdown.AddOptions(languageOptions);
        _languageDropdown.SetValueWithoutNotify(Array.IndexOf(LocalizationManager.SupportedLanguages, localization.CurrentLanguage));
        _languageDropdown.onValueChanged.AddListener(index => localization.SetLanguage(LocalizationManager.SupportedLanguages[index]));

        _btnClose.onClick.AddListener(Hide);
    }
}
