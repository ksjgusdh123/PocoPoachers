using System;
using UnityEngine;

public class LocalizationManager : Singleton<LocalizationManager>
{
    private const string PREF_LANGUAGE = "Settings.Language";

    public static readonly SystemLanguage[] SupportedLanguages =
    {
        SystemLanguage.Korean,
        SystemLanguage.English,
    };

    public SystemLanguage CurrentLanguage { get; private set; }

    public event Action OnLanguageChanged;

    protected override void Awake()
    {
        base.Awake();

        var saved = (SystemLanguage)PlayerPrefs.GetInt(PREF_LANGUAGE, (int)DefaultLanguage());
        CurrentLanguage = Array.IndexOf(SupportedLanguages, saved) >= 0 ? saved : SystemLanguage.English;
    }

    public void SetLanguage(SystemLanguage language)
    {
        if (CurrentLanguage == language) return;

        CurrentLanguage = language;
        PlayerPrefs.SetInt(PREF_LANGUAGE, (int)language);
        OnLanguageChanged?.Invoke();
    }

    public string GetString(string key)
    {
        var data = LocalizationTable.Instance.Get(key);
        if (data == null) return key;

        return CurrentLanguage switch
        {
            SystemLanguage.English => data.En,
            _ => data.Ko,
        };
    }

    private static SystemLanguage DefaultLanguage()
    {
        return Array.IndexOf(SupportedLanguages, Application.systemLanguage) >= 0
            ? Application.systemLanguage
            : SystemLanguage.English;
    }
}
