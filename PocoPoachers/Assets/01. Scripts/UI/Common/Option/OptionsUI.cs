using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ── Inspector 연결 구조 ──────────────────────────────────────────────────
//  OptionsUI   [UIFrame]
//  ├── Dimmer            [Image]
//  └── Panel
//      ├── Txt_Title     [TextMeshProUGUI]
//      ├── Btn_Close     [Button]
//      └── Content
//          ├── Slider_Master     [Slider]
//          ├── Slider_Bgm        [Slider]
//          ├── Slider_Sfx        [Slider]
//          └── Dropdown_Language [TMP_Dropdown]
// ────────────────────────────────────────────────────────────────────────

public class OptionsUI : UIBase
{
    private static readonly Dictionary<SystemLanguage, string> LanguageDisplayNames = new()
    {
        { SystemLanguage.Korean, "한국어" },
        { SystemLanguage.English, "English" },
    };

    [Header("Frame")]
    [SerializeField] private UIFrame _frame;

    [Header("Volume Sliders")]
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private TextMeshProUGUI _masterValueLabel;
    [SerializeField] private TextMeshProUGUI _bgmValueLabel;
    [SerializeField] private TextMeshProUGUI _sfxValueLabel;

    [Header("Language")]
    [SerializeField] private TMP_Dropdown _languageDropdown;

    protected override UIType UiType => UIType.Options;

    protected override void Awake()
    {
        base.Awake();

        var sound = SoundManager.GetInstance();
        BindVolumeSlider(_masterSlider, _masterValueLabel, sound.MasterVolume, sound.SetMasterVolume);
        BindVolumeSlider(_bgmSlider, _bgmValueLabel, sound.BgmVolume, sound.SetBgmVolume);
        BindVolumeSlider(_sfxSlider, _sfxValueLabel, sound.SfxVolume, sound.SetSfxVolume);

        var localization = LocalizationManager.GetInstance();

        var languageOptions = new List<string>();
        foreach (var language in LocalizationManager.SupportedLanguages)
            languageOptions.Add(LanguageDisplayNames.TryGetValue(language, out var name) ? name : language.ToString());

        _languageDropdown.ClearOptions();
        _languageDropdown.AddOptions(languageOptions);
        _languageDropdown.SetValueWithoutNotify(Array.IndexOf(LocalizationManager.SupportedLanguages, localization.CurrentLanguage));
        _languageDropdown.onValueChanged.AddListener(index => localization.SetLanguage(LocalizationManager.SupportedLanguages[index]));

        _frame.BtnClose.onClick.AddListener(Hide);
    }

    private static void BindVolumeSlider(
        Slider slider,
        TextMeshProUGUI valueLabel,
        float initialValue,
        Action<float> applyValue)
    {
        slider.SetValueWithoutNotify(initialValue);
        UpdateVolumeLabel(valueLabel, initialValue);
        slider.onValueChanged.AddListener(value =>
        {
            applyValue(value);
            UpdateVolumeLabel(valueLabel, value);
        });
    }

    private static void UpdateVolumeLabel(TextMeshProUGUI label, float value)
    {
        if (label != null)
            label.text = $"{Mathf.RoundToInt(value * 100f):00}%";
    }

}
